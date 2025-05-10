using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using GreenMeadowsPortal.Data; // Add this namespace

namespace GreenMeadowsPortal.Controllers
{
    [Authorize]
    public class ForumController : Controller
    {
        private readonly IForumService _forumService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ForumController> _logger;
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public ForumController(
            IForumService forumService,
            UserManager<ApplicationUser> userManager,
            ILogger<ForumController> logger,
            AppDbContext context,
            INotificationService notificationService)
        {
            _forumService = forumService ?? throw new ArgumentNullException(nameof(forumService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        // GET: Forum
        public async Task<IActionResult> Index(int page = 1, string sortBy = "recent", string search = "")
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var roles = await _userManager.GetRolesAsync(currentUser);
                var userRole = roles.FirstOrDefault() ?? "Homeowner";
                var isAdmin = roles.Contains("Admin");
                var isStaff = roles.Contains("Staff");

                var topics = await _forumService.GetAllTopicsAsync(page, sortBy, search);
                var totalTopics = await _forumService.GetTotalTopicCountAsync(search);
                var totalPages = (int)Math.Ceiling(totalTopics / 10.0);

                var topicViewModels = new List<ForumTopicViewModel>();
                foreach (var t in topics)
                {
                    var authorRoles = await _userManager.GetRolesAsync(t.Author);
                    var authorRole = authorRoles.FirstOrDefault() ?? "Homeowner";
                    topicViewModels.Add(new ForumTopicViewModel
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Content = t.Content,
                        AuthorId = t.AuthorId,
                        AuthorName = t.Author.FirstName + " " + t.Author.LastName,
                        AuthorRole = authorRole,
                        AuthorProfileImage = t.Author.ProfileImageUrl ?? "/images/default-avatar.png",
                        CreatedDate = t.CreatedDate,
                        LastActivityDate = t.LastActivityDate,
                        ViewCount = t.ViewCount,
                        ReplyCount = t.ReplyCount,
                        IsPinned = t.IsPinned,
                        IsClosed = t.IsClosed,
                        CanModerate = isAdmin || isStaff
                    });
                }

                var viewModel = new ForumIndexViewModel
                {
                    FirstName = currentUser.FirstName,
                    ProfileImageUrl = currentUser.ProfileImageUrl ?? "/images/default-avatar.png",
                    Role = userRole,
                    NotificationCount = 0, // TODO: Implement notification count
                    Topics = topicViewModels,
                    TotalTopics = totalTopics,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    SortBy = sortBy,
                    SearchQuery = search,
                    CanCreateNewTopic = true,
                    IsAdmin = isAdmin,
                    IsStaff = isStaff
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading forum index");
                TempData["ErrorMessage"] = "An error occurred while loading the forum. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Forum/Topic/5
        public async Task<IActionResult> Topic(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var roles = await _userManager.GetRolesAsync(currentUser);
                var userRole = roles.FirstOrDefault() ?? "Homeowner";
                var isAdmin = roles.Contains("Admin");
                var isStaff = roles.Contains("Staff");

                var topic = await _forumService.GetTopicByIdAsync(id);
                if (topic == null)
                {
                    return NotFound();
                }

                var authorRoles = await _userManager.GetRolesAsync(topic.Author);
                var authorRole = authorRoles.FirstOrDefault() ?? "Homeowner";

                var replies = new List<ReplyViewModel>();
                foreach (var r in topic.Replies)
                {
                    var replyAuthorRoles = await _userManager.GetRolesAsync(r.Author);
                    var replyAuthorRole = replyAuthorRoles.FirstOrDefault() ?? "Homeowner";
                    replies.Add(new ReplyViewModel
                    {
                        Id = r.Id,
                        Content = r.Content,
                        AuthorId = r.AuthorId,
                        AuthorName = r.Author.FirstName + " " + r.Author.LastName,
                        AuthorRole = replyAuthorRole,
                        AuthorProfileImage = r.Author.ProfileImageUrl ?? "/images/default-avatar.png",
                        CreatedDate = r.CreatedDate,
                        EditedDate = r.EditedDate,
                        CanEdit = r.AuthorId == currentUser.Id || isAdmin || isStaff,
                        CanDelete = r.AuthorId == currentUser.Id || isAdmin || isStaff
                    });
                }

                var viewModel = new TopicDetailsViewModel
                {
                    FirstName = currentUser.FirstName,
                    ProfileImageUrl = currentUser.ProfileImageUrl ?? "/images/default-avatar.png",
                    Role = userRole,
                    NotificationCount = 0, // TODO: Implement notification count
                    Id = topic.Id,
                    Title = topic.Title,
                    Content = topic.Content,
                    AuthorId = topic.AuthorId,
                    AuthorName = topic.Author.FirstName + " " + topic.Author.LastName,
                    AuthorRole = authorRole,
                    AuthorProfileImage = topic.Author.ProfileImageUrl ?? "/images/default-avatar.png",
                    CreatedDate = topic.CreatedDate,
                    LastActivityDate = topic.LastActivityDate,
                    ViewCount = topic.ViewCount,
                    ReplyCount = topic.ReplyCount,
                    IsPinned = topic.IsPinned,
                    IsClosed = topic.IsClosed,
                    Replies = replies,
                    CanReply = !topic.IsClosed || isAdmin,
                    CanModerate = isAdmin || isStaff,
                    CanPin = isAdmin,
                    CanClose = isAdmin || isStaff
                };

                return View("Details", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading topic {TopicId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the topic. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Forum/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var roles = await _userManager.GetRolesAsync(currentUser);
                var userRole = roles.FirstOrDefault() ?? "Homeowner";

                var viewModel = new CreateTopicViewModel
                {
                    FirstName = currentUser.FirstName,
                    ProfileImageUrl = currentUser.ProfileImageUrl ?? "/images/default-avatar.png",
                    Role = userRole,
                    NotificationCount = 0 // TODO: Implement notification count
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading create topic page");
                TempData["ErrorMessage"] = "An error occurred while loading the create topic page. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Forum/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTopicViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        var userRole = roles.FirstOrDefault() ?? "Homeowner";
                        model.FirstName = user.FirstName;
                        model.ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png";
                        model.Role = userRole;
                        model.NotificationCount = 0; // TODO: Implement notification count
                    }
                    return View(model);
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var topic = new ForumTopic
                {
                    Title = model.Title,
                    Content = model.Content,
                    AuthorId = currentUser.Id,
                    CreatedDate = DateTime.Now,
                    LastActivityDate = DateTime.Now
                };

                await _forumService.CreateTopicAsync(topic);
                TempData["SuccessMessage"] = "Topic created successfully!";
                return RedirectToAction(nameof(Topic), new { id = topic.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating topic");
                TempData["ErrorMessage"] = "An error occurred while creating the topic. Please try again later.";
                return View(model);
            }
        }

        // POST: Forum/Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int topicId, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    TempData["ErrorMessage"] = "Reply content cannot be empty.";
                    return RedirectToAction(nameof(Topic), new { id = topicId });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var reply = new ForumReply
                {
                    Content = content,
                    TopicId = topicId,
                    AuthorId = currentUser.Id,
                    CreatedDate = DateTime.Now
                };

                await _forumService.AddReplyAsync(reply);
                TempData["SuccessMessage"] = "Reply posted successfully!";
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while posting reply to topic {TopicId}", topicId);
                TempData["ErrorMessage"] = "An error occurred while posting your reply. Please try again later.";
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }
        }

        // POST: Forum/EditReply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReply(int replyId, int topicId, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    TempData["ErrorMessage"] = "Reply content cannot be empty.";
                    return RedirectToAction(nameof(Topic), new { id = topicId });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var roles = await _userManager.GetRolesAsync(currentUser);
                var isAdmin = roles.Contains("Admin");
                var isStaff = roles.Contains("Staff");

                var reply = await _forumService.GetReplyByIdAsync(replyId);
                if (reply == null)
                {
                    return NotFound();
                }

                if (reply.AuthorId != currentUser.Id && !isAdmin && !isStaff)
                {
                    TempData["ErrorMessage"] = "You don't have permission to edit this reply.";
                    return RedirectToAction(nameof(Topic), new { id = topicId });
                }

                reply.Content = content;
                reply.EditedDate = DateTime.Now;

                await _forumService.EditReplyAsync(reply);
                TempData["SuccessMessage"] = "Reply updated successfully!";
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while editing reply {ReplyId}", replyId);
                TempData["ErrorMessage"] = "An error occurred while updating your reply. Please try again later.";
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }
        }

        // POST: Forum/DeleteReply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReply(int replyId, int topicId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var roles = await _userManager.GetRolesAsync(currentUser);
                var isAdmin = roles.Contains("Admin");
                var isStaff = roles.Contains("Staff");

                var reply = await _forumService.GetReplyByIdAsync(replyId);
                if (reply == null)
                {
                    return NotFound();
                }

                if (reply.AuthorId != currentUser.Id && !isAdmin && !isStaff)
                {
                    TempData["ErrorMessage"] = "You don't have permission to delete this reply.";
                    return RedirectToAction(nameof(Topic), new { id = topicId });
                }

                await _forumService.DeleteReplyAsync(replyId);
                TempData["SuccessMessage"] = "Reply deleted successfully!";
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting reply {ReplyId}", replyId);
                TempData["ErrorMessage"] = "An error occurred while deleting the reply. Please try again later.";
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }
        }

        // POST: Forum/DeleteTopic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTopic(int topicId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var roles = await _userManager.GetRolesAsync(currentUser);
                var isAdmin = roles.Contains("Admin");
                var isStaff = roles.Contains("Staff");

                var topic = await _forumService.GetTopicByIdAsync(topicId);
                if (topic == null)
                {
                    return NotFound();
                }

                if (topic.AuthorId != currentUser.Id && !isAdmin && !isStaff)
                {
                    TempData["ErrorMessage"] = "You don't have permission to delete this topic.";
                    return RedirectToAction(nameof(Topic), new { id = topicId });
                }

                await _forumService.DeleteTopicAsync(topicId);
                TempData["SuccessMessage"] = "Topic deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting topic {TopicId}", topicId);
                TempData["ErrorMessage"] = "An error occurred while deleting the topic. Please try again later.";
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }
        }

        // POST: Forum/TogglePinTopic
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TogglePinTopic(int topicId)
        {
            try
            {
                var topic = await _forumService.GetTopicByIdAsync(topicId);
                if (topic == null)
                {
                    return NotFound();
                }

                await _forumService.TogglePinTopicAsync(topicId);
                TempData["SuccessMessage"] = topic.IsPinned ? "Topic unpinned successfully!" : "Topic pinned successfully!";
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while toggling pin status for topic {TopicId}", topicId);
                TempData["ErrorMessage"] = "An error occurred while updating the topic's pin status. Please try again later.";
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }
        }

        // POST: Forum/ToggleCloseTopic
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ToggleCloseTopic(int topicId)
        {
            try
            {
                var topic = await _forumService.GetTopicByIdAsync(topicId);
                if (topic == null)
                {
                    return NotFound();
                }

                await _forumService.ToggleCloseTopicAsync(topicId);
                TempData["SuccessMessage"] = topic.IsClosed ? "Topic reopened successfully!" : "Topic closed successfully!";
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while toggling close status for topic {TopicId}", topicId);
                TempData["ErrorMessage"] = "An error occurred while updating the topic's close status. Please try again later.";
                return RedirectToAction(nameof(Topic), new { id = topicId });
            }
        }

        // POST: Forum/Report
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Report(ReportContentViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please provide a valid reason for reporting.";
                    return RedirectToAction(nameof(Topic), new { id = model.TopicId });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var report = new ForumReport
                {
                    TopicId = model.TopicId,
                    ReplyId = model.ReplyId,
                    Reason = model.Reason,
                    ReportDate = DateTime.Now,
                    ReportedById = currentUser.Id
                };

                await _forumService.ReportContentAsync(report);

                // Notify all admins using the NotificationService
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in admins)
                {
                    await _notificationService.CreateNotificationAsync(
                        admin.Id,
                        $"A forum topic or reply was reported: {model.Reason}",
                        "ForumReport",
                        $"/Forum/Topic/{model.TopicId}"
                    );
                }

                TempData["SuccessMessage"] = "Content reported successfully. Our moderators will review it shortly.";
                return RedirectToAction(nameof(Topic), new { id = model.TopicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while reporting content");
                TempData["ErrorMessage"] = "An error occurred while reporting the content. Please try again later.";
                return RedirectToAction(nameof(Topic), new { id = model.TopicId });
            }
        }
    }
}