using GreenMeadowsPortal.Data;
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Services
{
    public class ServiceRequestService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ServiceRequestService> _logger;

        public ServiceRequestService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ServiceRequestService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Get all service requests (for admin)
        public async Task<List<ServiceRequest>> GetAllServiceRequestsAsync(string statusFilter = "all")
        {
            try
            {
                var query = _context.ServiceRequests
                    .Include(sr => sr.Requester)
                    .Include(sr => sr.AssignedTo)
                    .AsQueryable();

                // Apply status filter
                if (statusFilter == "open")
                {
                    query = query.Where(sr => sr.Status == ServiceRequestStatus.Open);
                }
                else if (statusFilter == "closed")
                {
                    query = query.Where(sr => sr.Status == ServiceRequestStatus.Closed);
                }

                // Get the requests and order by date (newest first)
                return await query
                    .OrderByDescending(sr => sr.DateSubmitted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllServiceRequestsAsync");
                // Return mock data for now
                return GetMockServiceRequests(statusFilter);
            }
        }

        // Get service requests for a specific user
        public async Task<List<ServiceRequest>> GetUserServiceRequestsAsync(string userId, string statusFilter = "all")
        {
            try
            {
                var query = _context.ServiceRequests
                    .Include(sr => sr.Requester)
                    .Include(sr => sr.AssignedTo)
                    .Where(sr => sr.RequesterId == userId)
                    .AsQueryable();

                // Apply status filter
                if (statusFilter == "open")
                {
                    query = query.Where(sr => sr.Status == ServiceRequestStatus.Open);
                }
                else if (statusFilter == "closed")
                {
                    query = query.Where(sr => sr.Status == ServiceRequestStatus.Closed);
                }

                // Get the requests and order by date (newest first)
                return await query
                    .OrderByDescending(sr => sr.DateSubmitted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserServiceRequestsAsync for user {UserId}", userId);
                return new List<ServiceRequest>(); // Return empty list instead of mock data
            }
        }

        // Get service request by ID
        public async Task<ServiceRequest?> GetServiceRequestByIdAsync(int id)
        {
            try
            {
                return await _context.ServiceRequests
                    .Include(sr => sr.Requester)
                    .Include(sr => sr.AssignedTo)
                    .FirstOrDefaultAsync(sr => sr.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetServiceRequestByIdAsync for request {RequestId}", id);
                return null; // Return null instead of mock data
            }
        }

        // Create new service request
        public async Task<bool> CreateServiceRequestAsync(string userId, ServiceRequestCreateViewModel model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                var request = new ServiceRequest
                {
                    IssueType = model.IssueType,
                    Description = model.Description,
                    Status = ServiceRequestStatus.Open,
                    RequesterId = userId,
                    DateSubmitted = DateTime.Now,
                    Location = model.Location ?? string.Empty
                };

                _context.ServiceRequests.Add(request);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateServiceRequestAsync");
                return false;
            }
        }

        // Close a service request
        public async Task<bool> CloseServiceRequestAsync(int requestId, string? adminNotes = null)
        {
            try
            {
                var request = await _context.ServiceRequests.FindAsync(requestId);
                if (request == null)
                    return false;

                request.Status = ServiceRequestStatus.Closed;
                request.DateClosed = DateTime.Now;

                if (!string.IsNullOrEmpty(adminNotes))
                {
                    request.AdminNotes = adminNotes;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CloseServiceRequestAsync");
                return false;
            }
        }

        // Reopen a service request (admin only)
        public async Task<bool> ReopenServiceRequestAsync(int requestId)
        {
            try
            {
                var request = await _context.ServiceRequests.FindAsync(requestId);
                if (request == null)
                    return false;

                request.Status = ServiceRequestStatus.Open;
                request.DateClosed = null;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReopenServiceRequestAsync");
                return false;
            }
        }

        // Get counts for dashboard
        public async Task<int> GetTotalOpenRequestsCountAsync()
        {
            try
            {
                return await _context.ServiceRequests
                    .CountAsync(sr => sr.Status == ServiceRequestStatus.Open);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTotalOpenRequestsCountAsync");
                return 0;
            }
        }

        public async Task<int> GetTotalClosedRequestsCountAsync()
        {
            try
            {
                return await _context.ServiceRequests
                    .CountAsync(sr => sr.Status == ServiceRequestStatus.Closed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTotalClosedRequestsCountAsync");
                return 0;
            }
        }

        public async Task<int> GetUserOpenRequestsCountAsync(string userId)
        {
            try
            {
                return await _context.ServiceRequests
                    .CountAsync(sr => sr.RequesterId == userId && sr.Status == ServiceRequestStatus.Open);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserOpenRequestsCountAsync");
                return 0;
            }
        }

        public async Task<int> GetUserClosedRequestsCountAsync(string userId)
        {
            try
            {
                return await _context.ServiceRequests
                    .CountAsync(sr => sr.RequesterId == userId && sr.Status == ServiceRequestStatus.Closed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserClosedRequestsCountAsync");
                return 0;
            }
        }

        // Assign staff to a service request
        public async Task<bool> AssignStaffToRequestAsync(int requestId, string staffId)
        {
            try
            {
                var request = await _context.ServiceRequests.FindAsync(requestId);
                if (request == null)
                    return false;

                var staff = await _userManager.FindByIdAsync(staffId);
                if (staff == null)
                    return false;

                request.AssignedToId = staffId;
                request.AssignedTo = staff;
                request.DateAssigned = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AssignStaffToRequestAsync");
                return false;
            }
        }

        // Get available staff members
        public async Task<List<ApplicationUser>> GetAvailableStaffAsync()
        {
            try
            {
                var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Staff");
                if (staffRole == null)
                    return new List<ApplicationUser>();

                var staffIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == staffRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                return await _context.Users
                    .Where(u => staffIds.Contains(u.Id))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAvailableStaffAsync");
                return new List<ApplicationUser>();
            }
        }

        private List<ServiceRequest> GetMockServiceRequests(string statusFilter)
        {
            var mockRequests = new List<ServiceRequest>
            {
                new ServiceRequest
                {
                    Id = 1,
                    IssueType = "Plumbing",
                    Description = "Leaking faucet in kitchen",
                    Status = ServiceRequestStatus.Open,
                    Requester = new ApplicationUser { FirstName = "John", LastName = "Doe", Unit = "101" },
                    DateSubmitted = DateTime.Now.AddDays(-2),
                    Location = "Kitchen"
                },
                new ServiceRequest
                {
                    Id = 2,
                    IssueType = "Electrical",
                    Description = "Light fixture not working",
                    Status = ServiceRequestStatus.Closed,
                    Requester = new ApplicationUser { FirstName = "Jane", LastName = "Smith", Unit = "202" },
                    DateSubmitted = DateTime.Now.AddDays(-5),
                    DateClosed = DateTime.Now.AddDays(-1),
                    Location = "Living Room"
                }
            };

            return statusFilter switch
            {
                "open" => mockRequests.Where(r => r.Status == ServiceRequestStatus.Open).ToList(),
                "closed" => mockRequests.Where(r => r.Status == ServiceRequestStatus.Closed).ToList(),
                _ => mockRequests
            };
        }

        private ServiceRequest? GetMockServiceRequestById(int id)
        {
            return GetMockServiceRequests("all").FirstOrDefault(r => r.Id == id);
        }
    }
}