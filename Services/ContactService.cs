using GreenMeadowsPortal.Data;
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GreenMeadowsPortal.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GreenMeadowsPortal.Controllers;

namespace GreenMeadowsPortal.Services
{
    public class ContactService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ContactController> _logger;

        public ContactService(
      AppDbContext context,
      UserManager<ApplicationUser> userManager,
      INotificationService notificationService,
       ILogger<ContactController> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
        }

        #region Contact Categories
        // Get all contact categories
        public async Task<List<ContactCategory>> GetAllContactCategoriesAsync()
        {
            try
            {
                return await _context.ContactCategories
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllContactCategoriesAsync: {ex.Message}");
                return GetMockContactCategories();
            }
        }

        // Get categories visible to staff
        public async Task<List<ContactCategory>> GetStaffVisibleCategoriesAsync()
        {
            try
            {
                return await _context.ContactCategories
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStaffVisibleCategoriesAsync: {ex.Message}");
                return GetMockContactCategories();
            }
        }

        // Get categories visible to homeowners
        public async Task<List<ContactCategory>> GetHomeownerVisibleCategoriesAsync()
        {
            try
            {
                return await _context.ContactCategories
                    .Where(c => c.IsPublic)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetHomeownerVisibleCategoriesAsync: {ex.Message}");
                return GetMockContactCategories().Where(c => c.IsPublic).ToList();
            }
        }

        // Add a new contact category
        public async Task<bool> AddContactCategoryAsync(string name, string description, bool isPublic)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    _logger.LogWarning("Cannot add contact category with empty name");
                    return false;
                }

                // Get the highest display order and add 1
                int maxOrder = 0;
                try
                {
                    maxOrder = await _context.ContactCategories.MaxAsync(c => (int?)c.DisplayOrder) ?? 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting max display order for contact categories");
                    // Continue with default value 0
                }

                var category = new ContactCategory
                {
                    Name = name,
                    Description = description ?? string.Empty,
                    IsPublic = isPublic,
                    DisplayOrder = maxOrder + 1
                };

                _context.ContactCategories.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully added contact category: {name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddContactCategoryAsync: {Message}", ex.Message);
                return false;
            }
        }

        // Delete a contact category
        public async Task<bool> DeleteContactCategoryAsync(int id)
        {
            var category = await _context.ContactCategories.FindAsync(id);
            if (category == null) return false;
            _context.ContactCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Department Contacts

        // Get all department contacts
        public async Task<List<DepartmentContactViewModel>> GetDepartmentContactsAsync()
        {
            try
            {
                var contacts = await _context.DepartmentContacts
                    .Include(d => d.User)
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                return contacts.Select(c => new DepartmentContactViewModel
                {
                    Id = c.Id,
                    DepartmentName = c.DepartmentName,
                    Description = c.Description,
                    ContactPerson = c.ContactPerson,
                    Position = c.Position,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber,
                    OfficeHours = c.OfficeHours,
                    Location = c.Location,
                    UserId = c.UserId ?? string.Empty,
                    ProfileImageUrl = c.User?.ProfileImageUrl ?? "/images/default-avatar.png"
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDepartmentContactsAsync: {ex.Message}");
                return GetMockDepartmentContacts();
            }
        }

        // Add a new department contact
        public async Task<bool> AddDepartmentContactAsync(string departmentName, string description, string contactPerson, string position, string email, string phoneNumber, string officeHours, string location)
        {
            try
            {
                if (string.IsNullOrEmpty(departmentName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phoneNumber))
                {
                    _logger.LogWarning("Cannot add department contact with empty required fields");
                    return false;
                }

                var departmentContact = new DepartmentContact
                {
                    DepartmentName = departmentName,
                    Description = description ?? string.Empty,
                    ContactPerson = contactPerson ?? string.Empty,
                    Position = position ?? string.Empty,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    OfficeHours = officeHours ?? string.Empty,
                    Location = location ?? string.Empty
                };

                _context.DepartmentContacts.Add(departmentContact);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully added department contact: {departmentName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddDepartmentContactAsync: {Message}", ex.Message);
                return false;
            }
        }

        // Delete a department contact by ID
        public async Task<bool> DeleteDepartmentContactAsync(int id)
        {
            var contact = await _context.DepartmentContacts.FindAsync(id);
            if (contact == null) return false;
            _context.DepartmentContacts.Remove(contact);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion
        public async Task<List<CommunityContactViewModel>> GetStaffContactsForMessagingAsync()
        {
            try
            {
                // Get users with Staff or Admin roles
                var staffRoleIds = await _userManager.GetUsersInRoleAsync("Staff");
                var adminRoleIds = await _userManager.GetUsersInRoleAsync("Admin");

                var staffUsers = staffRoleIds.Concat(adminRoleIds).Distinct();

                var staffContactsList = new List<CommunityContactViewModel>();

                foreach (var user in staffUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    staffContactsList.Add(new CommunityContactViewModel
                    {
                        UserId = user.Id,
                        FullName = $"{user.FirstName} {user.LastName}",
                        Email = user.Email ?? string.Empty,
                        PhoneNumber = user.PhoneNumber ?? string.Empty,
                        Address = string.Empty, // Never show staff address
                        Unit = string.Empty,
                        Role = roles.FirstOrDefault() ?? "Staff",
                        Department = user.Department ?? "General",
                        Bio = string.Empty,
                        ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                        ShowEmail = true,
                        ShowPhoneNumber = true,
                        ShowAddress = false,
                        CanMessage = true // Always allow messaging between staff
                    });
                }

                return staffContactsList.OrderBy(c => c.Department).ThenBy(c => c.FullName).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStaffContactsForMessagingAsync: {ex.Message}");
                return GetMockStaffContacts();
            }
        }
       
        #region Emergency Contacts
        public async Task<List<ContactMessageListingViewModel>> GetAllMessagesAsync()
        {
            try
            {
                var messages = await _context.ContactMessages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .OrderByDescending(m => m.SentDate)
                    .ToListAsync();

                return messages.Select(m => new ContactMessageListingViewModel
                {
                    MessageId = m.Id,
                    Subject = m.Subject,
                    SentDate = m.SentDate,
                    IsRead = m.IsRead,

                    SenderId = m.SenderId,
                    SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                    SenderProfileImage = m.Sender.ProfileImageUrl ?? "/images/default-avatar.png",

                    RecipientId = m.RecipientId,
                    RecipientName = $"{m.Recipient.FirstName} {m.Recipient.LastName}",
                    RecipientProfileImage = m.Recipient.ProfileImageUrl ?? "/images/default-avatar.png",

                    // For admins, we set this differently than just checking current user
                    IsFromCurrentUser = false
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllMessagesAsync: {ex.Message}");
                return GetMockMessages("admin");
            }
        }

        public async Task<List<ContactMessageListingViewModel>> GetStaffMessagesAsync(string staffId)
        {
            try
            {
                var messages = await _context.ContactMessages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Where(m => m.SenderId == staffId || m.RecipientId == staffId)
                    .OrderByDescending(m => m.SentDate)
                    .ToListAsync();

                return messages.Select(m => new ContactMessageListingViewModel
                {
                    MessageId = m.Id,
                    Subject = m.Subject,
                    SentDate = m.SentDate,
                    IsRead = m.IsRead,
                    IsFromCurrentUser = m.SenderId == staffId,

                    SenderId = m.SenderId,
                    SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                    SenderProfileImage = m.Sender.ProfileImageUrl ?? "/images/default-avatar.png",

                    RecipientId = m.RecipientId,
                    RecipientName = $"{m.Recipient.FirstName} {m.Recipient.LastName}",
                    RecipientProfileImage = m.Recipient.ProfileImageUrl ?? "/images/default-avatar.png"
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStaffMessagesAsync: {ex.Message}");
                return GetMockMessages(staffId);
            }
        }
        // Get all emergency contacts
        public async Task<List<EmergencyContactViewModel>> GetEmergencyContactsAsync()
        {
            try
            {
                var contacts = await _context.EmergencyContacts
                    .OrderBy(e => e.Priority)
                    .ThenBy(e => e.Name)
                    .ToListAsync();

                return contacts.Select(c => new EmergencyContactViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    Email = c.Email,
                    Description = c.Description,
                    Priority = c.Priority
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetEmergencyContactsAsync: {ex.Message}");
                return GetMockEmergencyContacts();
            }
        }

        // Add a new emergency contact
        public async Task<bool> AddEmergencyContactAsync(string name, string phoneNumber, string email, string description, int priority)
        {
            try
            {
                // Fully qualify the type to resolve ambiguity
                var emergencyContact = new GreenMeadowsPortal.Models.EmergencyContact
                {
                    Name = name,
                    PhoneNumber = phoneNumber,
                    Email = email,
                    Description = description,
                    Priority = priority
                };


                _context.EmergencyContacts.Add(emergencyContact);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddEmergencyContactAsync: {ex.Message}");
                return false;
            }
        }

        // Delete an emergency contact
        public async Task<bool> DeleteEmergencyContactAsync(int id)
        {
            var contact = await _context.EmergencyContacts.FindAsync(id);
            if (contact == null) return false;
            _context.EmergencyContacts.Remove(contact);
            await _context.SaveChangesAsync();
            return true;
        }
        #endregion

        #region Vendor Contacts

        // Get all vendor contacts
        public async Task<List<VendorContactViewModel>> GetVendorContactsAsync()
        {
            try
            {
                var contacts = await _context.VendorContacts
                    .OrderByDescending(v => v.IsPreferred)
                    .ThenBy(v => v.Service)
                    .ThenBy(v => v.CompanyName)
                    .ToListAsync();

                return contacts.Select(c => new VendorContactViewModel
                {
                    Id = c.Id,
                    CompanyName = c.CompanyName,
                    ContactPerson = c.ContactPerson,
                    PhoneNumber = c.PhoneNumber,
                    Email = c.Email,
                    Service = c.Service,
                    Website = c.Website,
                    Notes = c.Notes,
                    IsPreferred = c.IsPreferred
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetVendorContactsAsync: {ex.Message}");
                return GetMockVendorContacts();
            }
        }

        // Add a new vendor contact
        public async Task<bool> AddVendorContactAsync(string name, string contactPerson, string phoneNumber,
    string email, string service, string website, bool isPreferred)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(service))
                {
                    _logger.LogWarning("Cannot add vendor with empty required fields");
                    return false;
                }

                var vendorContact = new VendorContact
                {
                    CompanyName = name,
                    ContactPerson = contactPerson ?? string.Empty,
                    PhoneNumber = phoneNumber,
                    Email = email ?? string.Empty,
                    Service = service,
                    Website = website ?? string.Empty,
                    Notes = string.Empty,
                    IsPreferred = isPreferred
                };

                _context.VendorContacts.Add(vendorContact);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully added vendor contact: {name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddVendorContactAsync: {Message}", ex.Message);
                return false;
            }
        }


        // Delete a vendor contact
        public async Task<bool> DeleteVendorContactAsync(int id)
        {
            var contact = await _context.VendorContacts.FindAsync(id);
            if (contact == null) return false;
            _context.VendorContacts.Remove(contact);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Staff & Community Contacts

        // Get all staff contacts
        public async Task<List<CommunityContactViewModel>> GetStaffContactsAsync()
        {
            try
            {
                // Get users with Staff or Admin roles
                var staffRoleIds = await _userManager.GetUsersInRoleAsync("Staff");
                var adminRoleIds = await _userManager.GetUsersInRoleAsync("Admin");

                var staffUsers = staffRoleIds.Concat(adminRoleIds).Distinct();

                var staffContactsList = new List<CommunityContactViewModel>();

                foreach (var user in staffUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    staffContactsList.Add(new CommunityContactViewModel
                    {
                        UserId = user.Id,
                        FullName = $"{user.FirstName} {user.LastName}",
                        Email = user.ShowEmail() && !string.IsNullOrEmpty(user.Email) ? user.Email : string.Empty,
                        PhoneNumber = user.ShowPhoneNumber() && !string.IsNullOrEmpty(user.PhoneNumber) ? user.PhoneNumber : string.Empty,
                        Address = string.Empty, // Never show staff address
                        Unit = string.Empty,
                        Role = roles.FirstOrDefault() ?? "Staff",
                        Department = user.Department ?? "General",
                        Bio = string.Empty,
                        ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                        ShowEmail = user.ShowEmail(),
                        ShowPhoneNumber = user.ShowPhoneNumber(),
                        ShowAddress = false
                    });
                }

                return staffContactsList.OrderBy(c => c.Department).ThenBy(c => c.FullName).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStaffContactsAsync: {ex.Message}");
                return GetMockStaffContacts();
            }
        }

        // Get community contacts (residents who have opted in to directory)
        public async Task<List<CommunityContactViewModel>> GetCommunityContactsAsync(bool includePrivate = false)
        {
            try
            {
                var query = _context.CommunityContacts
                    .Include(c => c.User)
                    .AsQueryable();

                if (!includePrivate)
                {
                    query = query.Where(c => c.IsPublic);
                }

                var contacts = await query.ToListAsync();
                var communityContactsList = new List<CommunityContactViewModel>();

                foreach (var contact in contacts)
                {
                    var roles = contact.User != null
                        ? await _userManager.GetRolesAsync(contact.User)
                        : new List<string>();

                    communityContactsList.Add(new CommunityContactViewModel
                    {
                        UserId = contact.UserId,
                        FullName = $"{contact.User?.FirstName ?? "Unknown"} {contact.User?.LastName ?? "User"}",
                        Email = contact.ShowEmail && contact.User?.Email != null ? contact.User.Email : string.Empty,
                        PhoneNumber = contact.ShowPhoneNumber && contact.User?.PhoneNumber != null ? contact.User.PhoneNumber : string.Empty,
                        Address = contact.ShowAddress && contact.User?.Address != null ? contact.User.Address : string.Empty,
                        Unit = contact.ShowAddress && contact.User?.Unit != null ? contact.User.Unit : string.Empty,
                        Role = roles.FirstOrDefault() ?? "Homeowner",
                        Department = string.Empty,
                        Bio = contact.Bio,
                        ProfileImageUrl = contact.User?.ProfileImageUrl ?? "/images/default-avatar.png",
                        ShowEmail = contact.ShowEmail,
                        ShowPhoneNumber = contact.ShowPhoneNumber,
                        ShowAddress = contact.ShowAddress
                    });
                }

                return communityContactsList.OrderBy(c => c.FullName).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCommunityContactsAsync: {ex.Message}");
                return GetMockCommunityContacts(includePrivate);
            }
        }

        // Add a new community contact
        public async Task<bool> AddCommunityContactAsync(string userId, string bio, bool showPhoneNumber, bool showEmail, bool showAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Cannot add community contact with empty userId");
                    return false;
                }

                var communityContact = new CommunityContact
                {
                    UserId = userId,
                    Bio = bio ?? string.Empty,
                    ShowPhoneNumber = showPhoneNumber,
                    ShowEmail = showEmail,
                    ShowAddress = showAddress,
                    IsPublic = true // Default to public, adjust as needed
                };

                _context.CommunityContacts.Add(communityContact);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully added community contact for user: {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddCommunityContactAsync: {Message}", ex.Message);
                return false;
            }
        }

        // Delete a community contact by UserId (string)
        public async Task<bool> DeleteCommunityContactAsync(string userId)
        {
            var contact = await _context.CommunityContacts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (contact == null) return false;
            _context.CommunityContacts.Remove(contact);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Messaging

        // Send a message between users
        // Update the SendMessageAsync method in ContactService.cs
        public async Task<bool> SendMessageAsync(string senderId, string recipientId, string subject, string message)
        {
            try
            {
                // Validate the inputs
                if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(recipientId) ||
                    string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(message))
                {
                    _logger.LogWarning("SendMessageAsync called with invalid parameters");
                    return false;
                }

                // Check if users exist before attempting to send message
                var sender = await _context.Users.FindAsync(senderId);
                var recipient = await _context.Users.FindAsync(recipientId);

                if (sender == null || recipient == null)
                {
                    _logger.LogWarning($"SendMessageAsync: User not found - Sender: {senderId}, Recipient: {recipientId}");
                    return false;
                }

                // Create the message object
                var contactMessage = new ContactMessage
                {
                    SenderId = senderId,
                    RecipientId = recipientId,
                    Subject = subject,
                    Content = message,
                    SentDate = DateTime.Now,
                    IsRead = false,
                    DeletedBySender = false,
                    DeletedByRecipient = false
                };

                // Add to database
                _context.ContactMessages.Add(contactMessage);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Message sent successfully from {senderId} to {recipientId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessageAsync: {Message}", ex.Message);
                return false;
            }
        }

        // Get messages for a user (both sent and received)
        public async Task<int> GetUnreadMessageCountAsync(string userId)
        {
            try
            {
                return await _context.ContactMessages
                    .CountAsync(m => m.RecipientId == userId && !m.IsRead && !m.DeletedByRecipient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread message count: {Message}", ex.Message);
                return 0;
            }
        }
        public async Task<List<ContactMessageListingViewModel>> GetUserMessagesAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"Getting messages for user ID: {userId}");

                // Get all messages where the user is either sender or recipient
                // and respect the soft delete flags
                var messages = await _context.ContactMessages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Where(m =>
                        (m.SenderId == userId && !m.DeletedBySender) ||
                        (m.RecipientId == userId && !m.DeletedByRecipient))
                    .OrderByDescending(m => m.SentDate)
                    .ToListAsync();

                _logger.LogInformation($"Found {messages.Count} messages for user {userId}");

                // Map to view models with proper null checks
                var result = messages.Select(m => new ContactMessageListingViewModel
                {
                    MessageId = m.Id,
                    Subject = m.Subject,
                    SentDate = m.SentDate,
                    IsRead = m.IsRead,
                    IsFromCurrentUser = m.SenderId == userId,

                    SenderId = m.SenderId,
                    SenderName = m.Sender != null
                        ? $"{m.Sender.FirstName ?? ""} {m.Sender.LastName ?? ""}".Trim()
                        : "Unknown User",
                    SenderProfileImage = m.Sender?.ProfileImageUrl ?? "/images/default-avatar.png",

                    RecipientId = m.RecipientId,
                    RecipientName = m.Recipient != null
                        ? $"{m.Recipient.FirstName ?? ""} {m.Recipient.LastName ?? ""}".Trim()
                        : "Unknown User",
                    RecipientProfileImage = m.Recipient?.ProfileImageUrl ?? "/images/default-avatar.png"
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserMessagesAsync: {Message}", ex.Message);
                return new List<ContactMessageListingViewModel>();
            }
        }


        // Get a specific message by ID
        public async Task<ContactMessage?> GetMessageByIdAsync(int id)
        {
            try
            {
                return await _context.ContactMessages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMessageByIdAsync: {ex.Message}");
                return null;
            }
        }


        // Mark a message as read
        public async Task<bool> MarkMessageAsReadAsync(int id)
        {
            try
            {
                var message = await _context.ContactMessages.FindAsync(id);
                if (message == null)
                    return false;

                message.IsRead = true;
                message.ReadDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MarkMessageAsReadAsync: {ex.Message}");
                return false;
            }
        }

        // Delete a message (soft delete)
        // Fix the DeleteMessageAsync method in ContactService.cs
        public async Task<bool> DeleteMessageAsync(int id, string userId)
        {
            try
            {
                var message = await _context.ContactMessages.FindAsync(id);
                if (message == null)
                    return false;

                // Check if the user is the sender or recipient
                if (message.SenderId == userId)
                {
                    message.DeletedBySender = true;
                }
                else if (message.RecipientId == userId)
                {
                    message.DeletedByRecipient = true;
                }
                else
                {
                    return false;
                }

                // If both sender and recipient have deleted the message, hard delete it
                if (message.DeletedBySender && message.DeletedByRecipient)
                {
                    _context.ContactMessages.Remove(message);
                }
                else
                {
                    // Otherwise just update the flags
                    _context.Entry(message).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteMessageAsync: {Message}", ex.Message);
                return false;
            }
        }
        #endregion

        #region Helper Methods

        // Get user's role
        public async Task<string> GetUserRoleAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault() ?? "User";
        }

        #endregion

        #region Mock Data Methods (Used when database access fails)

        // Mock category data
        private List<ContactCategory> GetMockContactCategories()
        {
            return new List<ContactCategory>
            {
                new ContactCategory { Id = 1, Name = "Emergency", Description = "Emergency contact numbers", IsPublic = true, DisplayOrder = 1 },
                new ContactCategory { Id = 2, Name = "Administration", Description = "Administration department contacts", IsPublic = true, DisplayOrder = 2 },
                new ContactCategory { Id = 3, Name = "Maintenance", Description = "Maintenance department contacts", IsPublic = true, DisplayOrder = 3 },
                new ContactCategory { Id = 4, Name = "Security", Description = "Security department contacts", IsPublic = true, DisplayOrder = 4 },
                new ContactCategory { Id = 5, Name = "Vendors", Description = "Approved vendors and service providers", IsPublic = true, DisplayOrder = 5 },
                new ContactCategory { Id = 6, Name = "Resident Directory", Description = "Contact information for residents", IsPublic = false, DisplayOrder = 6 }
            };
        }

        // Mock department contacts
        private List<DepartmentContactViewModel> GetMockDepartmentContacts()
        {
            return new List<DepartmentContactViewModel>
            {
                new DepartmentContactViewModel
                {
                    Id = 1,
                    DepartmentName = "Administration Office",
                    Description = "Main administrative office for Green Meadows",
                    ContactPerson = "Emily Johnson",
                    Position = "Office Manager",
                    Email = "admin@greenmeadows.com",
                    PhoneNumber = "(555) 123-4567",
                    OfficeHours = "Monday-Friday: 8:00 AM - 5:00 PM",
                    Location = "Building A, Ground Floor",
                    UserId = "admin1",
                    ProfileImageUrl = "/images/default-avatar.png"
                },
                new DepartmentContactViewModel
                {
                    Id = 2,
                    DepartmentName = "Maintenance Department",
                    Description = "Handles all maintenance requests and repairs",
                    ContactPerson = "Michael Rodriguez",
                    Position = "Maintenance Supervisor",
                    Email = "maintenance@greenmeadows.com",
                    PhoneNumber = "(555) 123-4568",
                    OfficeHours = "Monday-Saturday: 7:00 AM - 4:00 PM",
                    Location = "Maintenance Building, East Side",
                    UserId = "maint1",
                    ProfileImageUrl = "/images/default-avatar.png"
                },
                new DepartmentContactViewModel
                {
                    Id = 3,
                    DepartmentName = "Security Office",
                    Description = "24/7 security services",
                    ContactPerson = "Robert Chen",
                    Position = "Head of Security",
                    Email = "security@greenmeadows.com",
                    PhoneNumber = "(555) 123-4569",
                    OfficeHours = "24/7",
                    Location = "Main Gate House",
                    UserId = "security1",
                    ProfileImageUrl = "/images/default-avatar.png"
                }
            };
        }

        // Mock emergency contacts
        private List<EmergencyContactViewModel> GetMockEmergencyContacts()
        {
            return new List<EmergencyContactViewModel>
            {
                new EmergencyContactViewModel
                {
                    Id = 1,
                    Name = "Emergency Services (Police, Fire, Ambulance)",
                    PhoneNumber = "911",
                    Email = "",
                    Description = "For life-threatening emergencies",
                    Priority = 1
                },
                new EmergencyContactViewModel
                {
                    Id = 2,
                    Name = "Green Meadows Security",
                    PhoneNumber = "(555) 123-4580",
                    Email = "security@greenmeadows.com",
                    Description = "24/7 on-site security",
                    Priority = 2
                },
                new EmergencyContactViewModel
                {
                    Id = 3,
                    Name = "Maintenance Emergency Line",
                    PhoneNumber = "(555) 123-4581",
                    Email = "maintenance@greenmeadows.com",
                    Description = "For urgent maintenance issues (water leaks, power outages, etc.)",
                    Priority = 3
                },
                new EmergencyContactViewModel
                {
                    Id = 4,
                    Name = "Local Hospital - City Medical Center",
                    PhoneNumber = "(555) 555-5555",
                    Email = "info@citymedical.com",
                    Description = "Nearest hospital - 3 miles from Green Meadows",
                    Priority = 4
                }
            };
        }

        // Mock vendor contacts
        private List<VendorContactViewModel> GetMockVendorContacts()
        {
            return new List<VendorContactViewModel>
            {
                new VendorContactViewModel
                {
                    Id = 1,
                    CompanyName = "Reliable Plumbing Services",
                    ContactPerson = "John Smith",
                    PhoneNumber = "(555) 234-5678",
                    Email = "info@reliableplumbing.com",
                    Service = "Plumbing",
                    Website = "www.reliableplumbing.com",
                    Notes = "Preferred vendor for all plumbing services",
                    IsPreferred = true
                },
                new VendorContactViewModel
                {
                    Id = 2,
                    CompanyName = "Green Thumb Landscaping",
                    ContactPerson = "Maria Garcia",
                    PhoneNumber = "(555) 345-6789",
                    Email = "info@greenthumb.com",
                    Service = "Landscaping",
                    Website = "www.greenthumb.com",
                    Notes = "Handles all community landscaping maintenance",
                    IsPreferred = true
                },
                new VendorContactViewModel
                {
                    Id = 3,
                    CompanyName = "Elite Security Systems",
                    ContactPerson = "Robert Johnson",
                    PhoneNumber = "(555) 456-7890",
                    Email = "sales@elitesecurity.com",
                    Service = "Security Systems",
                    Website = "www.elitesecurity.com",
                    Notes = "Installed and maintains all security cameras and access systems",
                    IsPreferred = true
                },
                new VendorContactViewModel
                {
                    Id = 4,
                    CompanyName = "City Power & Electric",
                    ContactPerson = "Customer Service",
                    PhoneNumber = "(555) 567-8901",
                    Email = "service@citypower.com",
                    Service = "Electricity",
                    Website = "www.citypower.com",
                    Notes = "Local utility company",
                    IsPreferred = false
                }
            };
        }

        // Mock staff contacts
        private List<CommunityContactViewModel> GetMockStaffContacts()
        {
            return new List<CommunityContactViewModel>
            {
                new CommunityContactViewModel
                {
                    UserId = "admin1",
                    FullName = "Emily Johnson",
                    Email = "admin@greenmeadows.com",
                    PhoneNumber = "(555) 123-4567",
                    Address = "",
                    Unit = "",
                    Role = "Admin",
                    Department = "Administration",
                    Bio = "",
                    ProfileImageUrl = "/images/default-avatar.png",
                    ShowEmail = true,
                    ShowPhoneNumber = true,
                    ShowAddress = false
                },
                new CommunityContactViewModel
                {
                    UserId = "maint1",
                    FullName = "Michael Rodriguez",
                    Email = "maintenance@greenmeadows.com",
                    PhoneNumber = "(555) 123-4568",
                    Address = "",
                    Unit = "",
                    Role = "Staff",
                    Department = "Maintenance",
                    Bio = "",
                    ProfileImageUrl = "/images/default-avatar.png",
                    ShowEmail = true,
                    ShowPhoneNumber = true,
                    ShowAddress = false
                },
                new CommunityContactViewModel
                {
                    UserId = "security1",
                    FullName = "Robert Chen",
                    Email = "security@greenmeadows.com",
                    PhoneNumber = "(555) 123-4569",
                    Address = "",
                    Unit = "",
                    Role = "Staff",
                    Department = "Security",
                    Bio = "",
                    ProfileImageUrl = "/images/default-avatar.png",
                    ShowEmail = true,
                    ShowPhoneNumber = true,
                    ShowAddress = false
                }
            };
        }

        // Mock community contacts
        private List<CommunityContactViewModel> GetMockCommunityContacts(bool includePrivate)
        {
            var contacts = new List<CommunityContactViewModel>
            {
                new CommunityContactViewModel
                {
                    UserId = "home1",
                    FullName = "Jennifer Williams",
                    Email = "jwilliams@example.com",
                    PhoneNumber = "(555) 987-6543",
                    Address = "123 Willow Lane",
                    Unit = "4B",
                    Role = "Homeowner",
                    Department = "",
                    Bio = "Resident since 2018, HOA committee member",
                    ProfileImageUrl = "/images/default-avatar.png",
                    ShowEmail = true,
                    ShowPhoneNumber = true,
                    ShowAddress = true
                },
                new CommunityContactViewModel
                {
                    UserId = "home2",
                    FullName = "Daniel Lee",
                    Email = "dlee@example.com",
                    PhoneNumber = "(555) 456-7890",
                    Address = "456 Oak Street",
                    Unit = "2A",
                    Role = "Homeowner",
                    Department = "",
                    Bio = "Social committee member, organizing community events",
                    ProfileImageUrl = "/images/default-avatar.png",
                    ShowEmail = true,
                    ShowPhoneNumber = false,
                    ShowAddress = false
                }
            };

            // Add a "private" contact visible only to admins
            if (includePrivate)
            {
                contacts.Add(new CommunityContactViewModel
                {
                    UserId = "home3",
                    FullName = "Sarah Martinez",
                    Email = "smartinez@example.com",
                    PhoneNumber = "(555) 789-0123",
                    Address = "789 Pine Court",
                    Unit = "1C",
                    Role = "Homeowner",
                    Department = "",
                    Bio = "New resident, moved in 2024",
                    ProfileImageUrl = "/images/default-avatar.png",
                    ShowEmail = false,
                    ShowPhoneNumber = false,
                    ShowAddress = false
                });
            }

            return contacts;
        }

        // Mock messages
        private List<ContactMessageListingViewModel> GetMockMessages(string userId)
        {
            return new List<ContactMessageListingViewModel>
            {
                new ContactMessageListingViewModel
                {
                    MessageId = 1,
                    Subject = "Question about community pool hours",
                    SentDate = DateTime.Now.AddDays(-2),
                    IsRead = true,
                    IsFromCurrentUser = userId == "home1",
                    SenderId = "home1",
                    SenderName = "Jennifer Williams",
                    SenderProfileImage = "/images/default-avatar.png",
                    RecipientId = "admin1",
                    RecipientName = "Emily Johnson",
                    RecipientProfileImage = "/images/default-avatar.png"
                },
                new ContactMessageListingViewModel
                {
                    MessageId = 2,
                    Subject = "Maintenance request follow-up",
                    SentDate = DateTime.Now.AddDays(-1),
                    IsRead = false,
                    IsFromCurrentUser = userId == "maint1",
                    SenderId = "maint1",
                    SenderName = "Michael Rodriguez",
                    SenderProfileImage = "/images/default-avatar.png",
                    RecipientId = "home2",
                    RecipientName = "Daniel Lee",
                    RecipientProfileImage = "/images/default-avatar.png"
                },
                new ContactMessageListingViewModel
                {
                    MessageId = 3,
                    Subject = "Community event coordination",
                    SentDate = DateTime.Now.AddHours(-5),
                    IsRead = false,
                    IsFromCurrentUser = userId == "admin1",
                    SenderId = "admin1",
                    SenderName = "Emily Johnson",
                    SenderProfileImage = "/images/default-avatar.png",
                    RecipientId = "home1",
                    RecipientName = "Jennifer Williams",
                    RecipientProfileImage = "/images/default-avatar.png"
                }
            };
        }

        #endregion
    }
}