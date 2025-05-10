using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    [Authorize]
    public class FeedbackController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FeedbackService _feedbackService;
        private readonly NotificationService _notificationService;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(
            UserManager<ApplicationUser> userManager,
            FeedbackService feedbackService,
            NotificationService notificationService,
            ILogger<FeedbackController> logger)
        {
            _userManager = userManager;
            _feedbackService = feedbackService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: /Feedback
        public async Task<IActionResult> Index(string statusFilter = "all", string typeFilter = "all", string searchQuery = "", int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var viewModel = await _feedbackService.GetFeedbackListAsync(
                user.Id,
                statusFilter,
                typeFilter,
                searchQuery,
                page
            );

            return View(viewModel);
        }

        // GET: /Feedback/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new FeedbackCreateViewModel
            {
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = roles.FirstOrDefault() ?? "Homeowner",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id)
            };

            return View(viewModel);
        }

        // POST: /Feedback/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FeedbackCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var feedbackId = await _feedbackService.CreateFeedbackAsync(model, user.Id);
                if (feedbackId > 0)
                {
                    TempData["SuccessMessage"] = "Your feedback has been submitted successfully. Thank you for your input!";

                    // Send confirmation email to user (optional)
                    // await SendFeedbackConfirmationEmail(user.Email, model.Subject);

                    return RedirectToAction(nameof(Details), new { id = feedbackId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to submit feedback. Please try again.";
                }
            }

            // If we got this far, something failed, redisplay form
            var roles = await _userManager.GetRolesAsync(user);
            model.FirstName = user.FirstName;
            model.ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png";
            model.Role = roles.FirstOrDefault() ?? "Homeowner";
            model.NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id);

            return View(model);
        }

        // GET: /Feedback/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var viewModel = await _feedbackService.GetFeedbackDetailsAsync(id, user.Id);
            if (viewModel == null)
            {
                TempData["ErrorMessage"] = "Feedback not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
        }

        // POST: /Feedback/Resolve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Resolve(int id, string adminResponse)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(adminResponse))
            {
                TempData["ErrorMessage"] = "Please provide a response before resolving.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var success = await _feedbackService.ResolveFeedbackAsync(id, user.Id, adminResponse);
            if (success)
            {
                TempData["SuccessMessage"] = "Feedback marked as resolved.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to resolve feedback.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Feedback/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var success = await _feedbackService.DeleteFeedbackAsync(id, user.Id);
            if (success)
            {
                TempData["SuccessMessage"] = "Feedback deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete feedback. You can only delete your own unresolved feedback.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Feedback/Dashboard (Admin/Staff only)
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var viewModel = await _feedbackService.GetDashboardStatisticsAsync(user.Id);
            return View(viewModel);
        }

        // API endpoint for mobile/AJAX
        [HttpGet]
        [Route("api/feedback")]
        public async Task<IActionResult> GetFeedbackApi(string? status = null, string? type = null, int page = 1, int pageSize = 10)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var viewModel = await _feedbackService.GetFeedbackListAsync(
                user.Id,
                status ?? "all",
                type ?? "all",
                "",
                page,
                pageSize
            );

            return Json(new
            {
                submissions = viewModel.Submissions,
                currentPage = viewModel.CurrentPage,
                totalPages = viewModel.TotalPages,
                totalCount = viewModel.TotalCount,
                newCount = viewModel.NewCount,
                resolvedCount = viewModel.ResolvedCount
            });
        }

        // Helper methods can be added for email notifications, additional validations, etc.
        private async Task SendFeedbackConfirmationEmail(string? email, string subject)
        {
            try
            {
                if (string.IsNullOrEmpty(email)) return;

                // Simulate an asynchronous email sending operation
                await Task.Run(() =>
                {
                    // Your email sending logic here
                    _logger.LogInformation($"Confirmation email sent for feedback: {subject}");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email");
            }
        }

    }
}
