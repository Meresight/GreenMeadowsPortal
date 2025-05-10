// Controllers/PollController.cs
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
    public class PollController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PollService _pollService;
        private readonly NotificationService _notificationService;
        private readonly ILogger<PollController> _logger;

        public PollController(
            UserManager<ApplicationUser> userManager,
            PollService pollService,
            NotificationService notificationService,
            ILogger<PollController> logger)
        {
            _userManager = userManager;
            _pollService = pollService;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: /Poll/
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "Homeowner";

            var viewModel = new PollListViewModel
            {
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                CurrentUserId = user.Id,
                CurrentUserRole = userRole,
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                ActivePolls = await _pollService.GetActivePollsForUserAsync(user.Id, userRole),
                CompletedPolls = await _pollService.GetCompletedPollsAsync(user.Id, userRole)
            };

            return View(viewModel);
        }

        // GET: /Poll/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "Homeowner";

            var poll = await _pollService.GetPollByIdAsync(id, user.Id);
            if (poll == null)
                return NotFound();

            poll.FirstName = user.FirstName;
            poll.ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png";
            poll.Role = userRole;
            poll.NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id);

            return View(poll);
        }

        // GET: /Poll/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new PollCreateViewModel
            {
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = roles.FirstOrDefault() ?? "Admin",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id)
            };

            return View(viewModel);
        }

        // POST: /Poll/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(PollCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var poll = new Poll
                {
                    Question = model.Question,
                    Description = model.Description,
                    CreatedDate = DateTime.Now,
                    ExpirationDate = model.ExpirationDate,
                    CreatedById = user.Id,
                    IsActive = true,
                    TargetAudience = model.TargetAudience
                };

                var id = await _pollService.CreatePollAsync(poll);
                if (id > 0)
                {
                    // Notify all eligible users
                    var allUsers = _userManager.Users.ToList();
                    foreach (var u in allUsers)
                    {
                        var userRoles = await _userManager.GetRolesAsync(u);
                        if (userRoles.Contains("Homeowner") || userRoles.Contains("Staff"))
                        {
                            await _notificationService.CreateNotificationAsync(
                                u.Id,
                                "New Poll Available",
                                $"A new poll has been created: {poll.Question}",
                                "Poll",
                                id.ToString()
                            );
                        }
                    }
                    TempData["SuccessMessage"] = "Poll created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to create poll. Please try again.");
                }
            }

            // If we got this far, something failed, redisplay form
            var roles = await _userManager.GetRolesAsync(user);
            model.FirstName = user.FirstName;
            model.ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png";
            model.Role = roles.FirstOrDefault() ?? "Admin";
            model.NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id);

            return View(model);
        }

        // POST: /Poll/Submit
        // Controllers/PollController.cs - Updated Submit action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(SubmitPollResponseViewModel model)
        {
            _logger.LogInformation($"Submit action called. PollId: {model.PollId}, Response: {model.Response}");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not authenticated");
                return RedirectToAction("Login", "Account");
            }

            if (model.PollId <= 0)
            {
                _logger.LogWarning($"Invalid poll ID: {model.PollId}");
                return BadRequest("Invalid poll ID");
            }

            _logger.LogInformation($"Submitting vote for poll {model.PollId} by user {user.Id}: {model.Response}");

            // Call the service method to submit the response
            var success = await _pollService.SubmitPollResponseAsync(model.PollId, user.Id, model.Response);

            if (success)
            {
                _logger.LogInformation($"Vote submitted successfully for poll {model.PollId}");
                TempData["SuccessMessage"] = "Your response has been recorded.";
            }
            else
            {
                _logger.LogWarning($"Failed to submit vote for poll {model.PollId}");
                TempData["ErrorMessage"] = "Failed to submit your response. Please try again.";
            }

            // Important: Redirect to Details to see the updated poll
            return RedirectToAction(nameof(Details), new { id = model.PollId });
        }

        // POST: /Poll/Close/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Close(int id)
        {
            var success = await _pollService.ClosePollAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Poll closed successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to close poll.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Poll/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _pollService.DeletePollAsync(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Poll deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete poll.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}