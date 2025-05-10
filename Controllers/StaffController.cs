using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    [Authorize(Roles = "Staff")]
    public class StaffController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public StaffController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // This matches the route expected by your view: /Staff/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");

            // Create sample staff profile data (in a real app, get this from your database)
            var staffProfile = new StaffProfileViewModel
            {
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = "Staff Member",
                Department = "Maintenance",
                EmployeeId = "EMP" + new Random().Next(1000, 9999),
                Position = "Maintenance Technician",
                HireDate = new DateTime(2023, 4, 15),
                Status = "Active",
                NotificationCount = 3,

                // Emergency contacts
                PrimaryContactName = "Jane Doe",
                PrimaryContactRelationship = "Spouse",
                PrimaryContactPhone = "(555) 123-4567",
                PrimaryContactEmail = "jane.doe@example.com",

                SecondaryContactName = "John Smith",
                SecondaryContactRelationship = "Parent",
                SecondaryContactPhone = "(555) 987-6543",
                SecondaryContactEmail = "john.smith@example.com",

                // Sample skills
                Skills = new List<StaffSkill>
                {
                    new StaffSkill
                    {
                        Id = 1,
                        Name = "Plumbing",
                        Type = "Technical",
                        Level = "Advanced",
                        AcquiredDate = new DateTime(2020, 5, 10),
                        ExpiryDate = null,
                        Status = "Active"
                    },
                    new StaffSkill
                    {
                        Id = 2,
                        Name = "Electrical Maintenance",
                        Type = "Technical",
                        Level = "Intermediate",
                        AcquiredDate = new DateTime(2021, 3, 15),
                        ExpiryDate = new DateTime(2025, 3, 15),
                        Status = "Active"
                    },
                    new StaffSkill
                    {
                        Id = 3,
                        Name = "CPR Certification",
                        Type = "Safety",
                        Level = "Certified",
                        AcquiredDate = new DateTime(2022, 1, 5),
                        ExpiryDate = new DateTime(2024, 1, 5),
                        Status = "Active"
                    }
                },

                // Work schedule
                Schedule = new WorkSchedule
                {
                    Monday = "8:00 AM - 4:00 PM",
                    Tuesday = "8:00 AM - 4:00 PM",
                    Wednesday = "8:00 AM - 4:00 PM",
                    Thursday = "8:00 AM - 4:00 PM",
                    Friday = "8:00 AM - 4:00 PM",
                    Saturday = "Off",
                    Sunday = "Off",
                    Notes = "Available for on-call emergencies on weekends."
                }
            };

            return View(staffProfile);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(StaffProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return View("Profile", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");

            // Update user properties
            user.FirstName = model.FullName.Split(' ')[0];
            user.LastName = string.Join(" ", model.FullName.Split(' ').Skip(1));
            user.PhoneNumber = model.PhoneNumber;

            // Handle profile image upload
            if (model.ProfileImage != null)
            {
                // In a real app, you would save the file and update the path
                string uniqueFileName = Guid.NewGuid() + "_" + model.ProfileImage.FileName;
                user.ProfileImageUrl = "/images/" + uniqueFileName;
                model.ProfileImageUrl = user.ProfileImageUrl;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error updating profile: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Profile");
        }

        // Add Dashboard/Home view for Staff
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");

            // Redirect to the Dashboard controller's StaffDashboard action
            return RedirectToAction("StaffDashboard", "Dashboard");
        }

        // Additional methods for emergency contacts, skills, etc. can be added here
    }
}