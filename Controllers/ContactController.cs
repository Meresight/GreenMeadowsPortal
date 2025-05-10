using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    [Authorize]
    public class ContactController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly ContactService _contactService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService,
            ContactService contactService,
            ILogger<ContactController> logger)
        {
            _userManager = userManager;
            _notificationService = notificationService;
            _contactService = contactService;
            _logger = logger;
        }
        // Controllers/ContactController.cs (add this method)
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> StaffDirectory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new ContactDirectoryViewModel
            {
                CurrentUser = user,
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = roles.FirstOrDefault() ?? "Staff",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                StaffContacts = await _contactService.GetStaffContactsForMessagingAsync()
            };

            return View(viewModel);
        }
        // GET: /Contact - Main contact directory
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "Homeowner";

            // Get different contact categories based on user role
            var viewModel = new ContactDirectoryViewModel
            {
                CurrentUser = user,
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = userRole,
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id)
            };

            // Get contact directories based on role
            if (userRole == "Admin")
            {
                // Admins see all contacts
                viewModel.ContactCategories = await _contactService.GetAllContactCategoriesAsync();
                viewModel.StaffContacts = await _contactService.GetStaffContactsAsync();
                viewModel.EmergencyContacts = await _contactService.GetEmergencyContactsAsync();
                viewModel.VendorContacts = await _contactService.GetVendorContactsAsync();
                viewModel.DepartmentContacts = await _contactService.GetDepartmentContactsAsync();
                viewModel.CommunityContacts = await _contactService.GetCommunityContactsAsync(true); // Include private
            }
            else if (userRole == "Staff")
            {
                // Staff see staff, department, emergency, and vendor contacts
                viewModel.ContactCategories = await _contactService.GetStaffVisibleCategoriesAsync();
                viewModel.StaffContacts = await _contactService.GetStaffContactsAsync();
                viewModel.EmergencyContacts = await _contactService.GetEmergencyContactsAsync();
                viewModel.VendorContacts = await _contactService.GetVendorContactsAsync();
                viewModel.DepartmentContacts = await _contactService.GetDepartmentContactsAsync();
                viewModel.CommunityContacts = await _contactService.GetCommunityContactsAsync(false); // Exclude private
            }
            else
            {
                // Homeowners see only publicly visible contacts
                viewModel.ContactCategories = await _contactService.GetHomeownerVisibleCategoriesAsync();
                viewModel.EmergencyContacts = await _contactService.GetEmergencyContactsAsync();
                viewModel.DepartmentContacts = await _contactService.GetDepartmentContactsAsync();
                viewModel.CommunityContacts = await _contactService.GetCommunityContactsAsync(false); // Exclude private
            }

            return View(viewModel);
        }

        // GET: /Contact/Message/{id} - Send message to a contact
        // Update the Message action in ContactController.cs
        // GET: /Contact/Message/{id} - Send message to a contact
        // Updated Message action to fix permission issues
        [HttpGet]
        public async Task<IActionResult> Message(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            var contactUser = await _userManager.FindByIdAsync(id);
            if (contactUser == null)
                return NotFound("User not found");

            // Get roles for both users
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            var contactUserRoles = await _userManager.GetRolesAsync(contactUser);

            var currentUserRole = currentUserRoles.FirstOrDefault() ?? "Homeowner";
            var contactUserRole = contactUserRoles.FirstOrDefault() ?? "Homeowner";

            // Set messaging permissions based on roles
            bool canMessage = false;

            // Admin can message anyone, including other admins and staff
            if (currentUserRole == "Admin")
            {
                canMessage = true;
            }
            // Staff can message staff, admin, and homeowners
            else if (currentUserRole == "Staff")
            {
                canMessage = true;
            }
            // Homeowners can message staff and admin
            else if (currentUserRole == "Homeowner" &&
                     (contactUserRole == "Staff" || contactUserRole == "Admin"))
            {
                canMessage = true;
            }

            if (!canMessage)
            {
                TempData["ErrorMessage"] = "You do not have permission to message this user.";
                return RedirectToAction("Index");
            }

            var messageModel = new ContactMessageViewModel
            {
                CurrentUser = currentUser,
                ContactUser = contactUser,
                FirstName = currentUser.FirstName,
                ProfileImageUrl = currentUser.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = currentUserRole,
                NotificationCount = await _notificationService.GetUnreadCountAsync(currentUser.Id),

                // Contact details
                ContactId = contactUser.Id,
                ContactName = $"{contactUser.FirstName} {contactUser.LastName}",
                ContactEmail = contactUser.Email ?? string.Empty,
                ContactRole = contactUserRole,
                ContactDepartment = contactUser.Department ?? "General",
                ContactImageUrl = contactUser.ProfileImageUrl ?? "/images/default-avatar.png"
            };

            return View(messageModel);
        }

        // POST: /Contact/SendMessage - Process sending a message
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(ContactMessageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill out all required fields.";
                return RedirectToAction("Message", new { id = model.ContactId });
            }

            var sender = await _userManager.GetUserAsync(User);
            if (sender == null)
                return RedirectToAction("Login", "Account");

            var recipient = await _userManager.FindByIdAsync(model.ContactId);
            if (recipient == null)
                return NotFound();

            // Create and send the message
            var success = await _contactService.SendMessageAsync(
                senderId: sender.Id,
                recipientId: recipient.Id,
                subject: model.Subject,
                message: model.Message
            );

            if (success)
            {
                // Create notification for recipient
                await _notificationService.CreateNotificationAsync(
                    userId: recipient.Id,
                    title: "New Message",
                    message: $"You have received a new message from {sender.FirstName} {sender.LastName}.",
                    type: "Message",
                    referenceId: sender.Id
                );

                TempData["SuccessMessage"] = "Message sent successfully!";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to send message. Please try again later.";
                return RedirectToAction("Message", new { id = model.ContactId });
            }
        }

        // GET: /Contact/Inbox - View user's inbox
        public async Task<IActionResult> Inbox()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return RedirectToAction("Login", "Account");

                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault() ?? "Homeowner";

                // Log to debug
                _logger.LogInformation($"Loading inbox for user {user.Id} with role {userRole}");

                // Get messages based on user role
                var viewModel = new ContactInboxViewModel
                {
                    CurrentUser = user,
                    FirstName = user.FirstName ?? string.Empty,
                    ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                    Role = userRole,
                    NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id)
                };

                if (userRole == "Admin")
                {
                    // Admins see all messages
                    viewModel.Messages = await _contactService.GetAllMessagesAsync();
                    viewModel.InboxTitle = "Admin Inbox";
                    viewModel.CanManageAllMessages = true;
                }
                else if (userRole == "Staff")
                {
                    // Staff see messages to/from them
                    viewModel.Messages = await _contactService.GetStaffMessagesAsync(user.Id);
                    viewModel.InboxTitle = "Staff Inbox";
                    viewModel.CanManageAllMessages = false;
                }
                else
                {
                    // Homeowners see only their own messages
                    viewModel.Messages = await _contactService.GetUserMessagesAsync(user.Id);
                    viewModel.InboxTitle = "Message Inbox";
                    viewModel.CanManageAllMessages = false;
                }

                _logger.LogInformation($"Found {viewModel.Messages.Count} messages for user {user.Id}");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Inbox action: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while loading your inbox. Please try again.";
                return RedirectToAction("Index", "Dashboard");
            }
        }


        // GET: /Contact/ViewMessage/{id} - View a specific message
        public async Task<IActionResult> ViewMessage(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return RedirectToAction("Login", "Account");

                var message = await _contactService.GetMessageByIdAsync(id);
                if (message == null)
                {
                    TempData["ErrorMessage"] = "Message not found.";
                    return RedirectToAction("Inbox");
                }

                // Check if user is the sender or recipient
                if (message.SenderId != user.Id && message.RecipientId != user.Id)
                {
                    TempData["ErrorMessage"] = "You do not have permission to view this message.";
                    return RedirectToAction("Inbox");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var sender = await _userManager.FindByIdAsync(message.SenderId);
                var recipient = await _userManager.FindByIdAsync(message.RecipientId);

                var viewMessageModel = new ViewMessageViewModel
                {
                    CurrentUser = user,
                    FirstName = user.FirstName ?? string.Empty,
                    ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                    Role = roles.FirstOrDefault() ?? "Homeowner",
                    NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),

                    // Message details
                    MessageId = message.Id,
                    Subject = message.Subject,
                    MessageContent = message.Content,
                    SentDate = message.SentDate,
                    IsRead = message.IsRead,

                    // Sender details
                    SenderId = message.SenderId,
                    SenderName = sender != null ? $"{sender.FirstName ?? ""} {sender.LastName ?? ""}".Trim() : "Unknown User",
                    SenderEmail = sender?.Email ?? "",
                    SenderRole = sender != null ? (await _userManager.GetRolesAsync(sender)).FirstOrDefault() ?? "User" : "Unknown",
                    SenderImageUrl = sender?.ProfileImageUrl ?? "/images/default-avatar.png",

                    // Recipient details
                    RecipientId = message.RecipientId,
                    RecipientName = recipient != null ? $"{recipient.FirstName ?? ""} {recipient.LastName ?? ""}".Trim() : "Unknown User",
                    RecipientEmail = recipient?.Email ?? "",
                    RecipientRole = recipient != null ? (await _userManager.GetRolesAsync(recipient)).FirstOrDefault() ?? "User" : "Unknown",
                    RecipientImageUrl = recipient?.ProfileImageUrl ?? "/images/default-avatar.png"
                };

                // Mark as read if the current user is the recipient and message is unread
                if (message.RecipientId == user.Id && !message.IsRead)
                {
                    await _contactService.MarkMessageAsReadAsync(id);
                    viewMessageModel.IsRead = true;
                }

                return View(viewMessageModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ViewMessage action: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while loading the message. Please try again.";
                return RedirectToAction("Inbox");
            }
        }

        // POST: /Contact/MarkAsRead/{id} - Mark a message as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var message = await _contactService.GetMessageByIdAsync(id);
            if (message == null)
                return NotFound();

            // Check if user is the recipient
            if (message.RecipientId != user.Id)
                return Forbid();

            await _contactService.MarkMessageAsReadAsync(id);
            return RedirectToAction(nameof(ViewMessage), new { id });
        }

        // POST: /Contact/MarkAllAsRead - Mark all messages as read
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Get all unread messages for the user
            var messages = await _contactService.GetUserMessagesAsync(user.Id);
            foreach (var message in messages.Where(m => !m.IsRead && !m.IsFromCurrentUser))
            {
                await _contactService.MarkMessageAsReadAsync(message.MessageId);
            }

            TempData["SuccessMessage"] = "All messages marked as read.";
            return RedirectToAction(nameof(Inbox));
        }

        // POST: /Contact/DeleteMessage/{id} - Delete a message
        // Update the DeleteMessage method in ContactController.cs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return RedirectToAction("Login", "Account");

                var message = await _contactService.GetMessageByIdAsync(id);
                if (message == null)
                {
                    TempData["ErrorMessage"] = "Message not found.";
                    return RedirectToAction("Inbox");
                }

                // Check if user is the sender or recipient
                if (message.SenderId != user.Id && message.RecipientId != user.Id)
                {
                    TempData["ErrorMessage"] = "You do not have permission to delete this message.";
                    return RedirectToAction("Inbox");
                }

                // Delete the message
                var success = await _contactService.DeleteMessageAsync(id, user.Id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Message deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete message.";
                }

                return RedirectToAction("Inbox");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the message: " + ex.Message;
                return RedirectToAction("Inbox");
            }
        }

        // Admin only methods for managing contacts

        // GET: /Contact/Manage - Admin contact management
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new ManageContactsViewModel
            {
                FirstName = user.FirstName ?? "Admin",
                Role = roles.FirstOrDefault() ?? "Admin",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                ContactCategories = await _contactService.GetAllContactCategoriesAsync(),
                DepartmentContacts = await _contactService.GetDepartmentContactsAsync(),
                EmergencyContacts = await _contactService.GetEmergencyContactsAsync(),
                VendorContacts = await _contactService.GetVendorContactsAsync(),
                CommunityContacts = await _contactService.GetCommunityContactsAsync(true),
                AvailableUsers = await _userManager.Users.ToListAsync()
            };

            return View(viewModel);
        }

        // POST: /Contact/AddCategory - Add a new contact category
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddCategory([Bind(Prefix = "AddCategoryModel")] AddContactCategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide all required information.";
                return RedirectToAction("Manage");
            }

            var success = await _contactService.AddContactCategoryAsync(
                name: model.CategoryName,
                description: model.Description,
                isPublic: model.IsPublic
            );

            if (success)
            {
                TempData["SuccessMessage"] = "Contact category added successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to add contact category.";
            }

            return RedirectToAction("Manage");
        }

        // POST: /Contact/AddEmergencyContact - Add a new emergency contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddEmergencyContact(AddEmergencyContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide all required information.";
                return RedirectToAction("Manage");
            }

            var success = await _contactService.AddEmergencyContactAsync(
                name: model.Name,
                phoneNumber: model.PhoneNumber,
                email: model.Email,
                description: model.Description,
                priority: model.Priority
            );

            if (success)
            {
                TempData["SuccessMessage"] = "Emergency contact added successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to add emergency contact.";
            }

            return RedirectToAction("Manage");
        }

        // POST: /Contact/AddVendorContact - Add a new vendor contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddVendorContact([Bind(Prefix = "AddVendorModel")] AddVendorContactViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please provide all required information.";
                    return RedirectToAction("Manage");
                }

                _logger.LogInformation($"Adding vendor contact: {model.CompanyName}");

                var success = await _contactService.AddVendorContactAsync(
                    name: model.CompanyName,
                    contactPerson: model.ContactPerson,
                    phoneNumber: model.PhoneNumber,
                    email: model.Email,
                    service: model.Service,
                    website: model.Website,
                    isPreferred: model.IsPreferred
                );

                if (success)
                {
                    TempData["SuccessMessage"] = "Vendor contact added successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to add vendor contact.";
                }

                return RedirectToAction("Manage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddVendorContact action: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An unexpected error occurred while adding the vendor contact: " + ex.Message;
                return RedirectToAction("Manage");
            }
        }
        // First, let's fix the AddContactCategory method in the ContactController.cs
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddContactCategory(AddContactCategoryViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please provide all required information.";
                    return RedirectToAction("Manage");
                }

                _logger.LogInformation($"Adding contact category: {model.CategoryName}");

                var success = await _contactService.AddContactCategoryAsync(
                    name: model.CategoryName,
                    description: model.Description,
                    isPublic: model.IsPublic
                );

                if (success)
                {
                    TempData["SuccessMessage"] = "Contact category added successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to add contact category.";
                }

                return RedirectToAction("Manage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddContactCategory action: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An unexpected error occurred while adding the contact category: " + ex.Message;
                return RedirectToAction("Manage");
            }
        }

        // POST: /Contact/DeleteContact - Delete a contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteContact(string id, string type)
        {
            bool success = false;

            switch (type.ToLower())
            {
                case "emergency":
                case "vendor":
                case "category":
                case "department":
                    if (int.TryParse(id, out int intId))
                    {
                        switch (type.ToLower())
                        {
                            case "emergency":
                                success = await _contactService.DeleteEmergencyContactAsync(intId);
                                break;
                            case "vendor":
                                success = await _contactService.DeleteVendorContactAsync(intId);
                                break;
                            case "category":
                                success = await _contactService.DeleteContactCategoryAsync(intId);
                                break;
                            case "department":
                                success = await _contactService.DeleteDepartmentContactAsync(intId);
                                break;
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Invalid contact ID.";
                        return RedirectToAction("Manage");
                    }
                    break;
                case "community":
                    success = await _contactService.DeleteCommunityContactAsync(id);
                    break;
                default:
                    TempData["ErrorMessage"] = "Invalid contact type.";
                    return RedirectToAction("Manage");
            }

            if (success)
            {
                TempData["SuccessMessage"] = "Contact deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete contact.";
            }

            return RedirectToAction("Manage");
        }

        // POST: /Contact/AddDepartmentContact - Add a new department contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddDepartmentContact([Bind(Prefix = "AddDepartmentModel")] AddDepartmentContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide all required information.";
                return RedirectToAction("Manage");
            }

            var success = await _contactService.AddDepartmentContactAsync(
                departmentName: model.DepartmentName,
                description: model.Description,
                contactPerson: model.ContactPerson,
                position: model.Position,
                email: model.Email,
                phoneNumber: model.PhoneNumber,
                officeHours: model.OfficeHours,
                location: model.Location
            );

            if (success)
            {
                TempData["SuccessMessage"] = "Department contact added successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to add department contact.";
            }

            return RedirectToAction("Manage");
        }

        // POST: /Contact/AddCommunityContact - Add a new community contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddCommunityContact([Bind(Prefix = "AddCommunityModel")] AddCommunityContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide all required information.";
                return RedirectToAction("Manage");
            }

            var success = await _contactService.AddCommunityContactAsync(
                userId: model.UserId,
                bio: model.Bio,
                showPhoneNumber: model.ShowPhoneNumber,
                showEmail: model.ShowEmail,
                showAddress: model.ShowAddress
            );

            if (success)
            {
                TempData["SuccessMessage"] = "Community contact added successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to add community contact.";
            }

            return RedirectToAction("Manage");
        }
    }
}