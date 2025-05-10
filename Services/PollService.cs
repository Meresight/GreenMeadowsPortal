// Services/PollService.cs
using GreenMeadowsPortal.Data;
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Services
{
    public class PollService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PollService> _logger;

        public PollService(AppDbContext context, ILogger<PollService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Get all active polls for a user based on their role
        public async Task<List<PollViewModel>> GetActivePollsForUserAsync(string userId, string userRole)
        {
            try
            {
                var query = _context.Set<Poll>()
                    .Include(p => p.CreatedBy)
                    .Include(p => p.Responses)
                    .Where(p => p.IsActive &&
                               (p.ExpirationDate == null || p.ExpirationDate > DateTime.Now));

                // Filter by target audience based on user role
                if (userRole == "Admin")
                {
                    // Admins can see all polls
                }
                else if (userRole == "Staff")
                {
                    // Staff can see polls for All and Staff
                    query = query.Where(p => p.TargetAudience == "All" || p.TargetAudience == "Staff");
                }
                else
                {
                    // Homeowners can see polls for All and Homeowners
                    query = query.Where(p => p.TargetAudience == "All" || p.TargetAudience == "Homeowners");
                }

                var polls = await query.OrderByDescending(p => p.CreatedDate).ToListAsync();
                return MapPollsToViewModels(polls, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active polls for user {UserId}", userId);
                return new List<PollViewModel>();
            }
        }

        // Get completed (expired or inactive) polls for admin/staff
        public async Task<List<PollViewModel>> GetCompletedPollsAsync(string userId, string userRole)
        {
            try
            {
                var query = _context.Set<Poll>()
                    .Include(p => p.CreatedBy)
                    .Include(p => p.Responses)
                    .Where(p => !p.IsActive ||
                                (p.ExpirationDate != null && p.ExpirationDate <= DateTime.Now));

                // Filter by user role for visibility
                if (userRole == "Admin")
                {
                    // Admins can see all completed polls
                }
                else if (userRole == "Staff")
                {
                    // Staff can see polls for All and Staff
                    query = query.Where(p => p.TargetAudience == "All" || p.TargetAudience == "Staff");
                }
                else
                {
                    // Homeowners can see polls for All and Homeowners
                    query = query.Where(p => p.TargetAudience == "All" || p.TargetAudience == "Homeowners");
                }

                var polls = await query.OrderByDescending(p => p.CreatedDate).ToListAsync();
                return MapPollsToViewModels(polls, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving completed polls");
                return new List<PollViewModel>();
            }
        }

        // Get a specific poll by ID
        public async Task<PollDetailsViewModel?> GetPollByIdAsync(int id, string userId)
        {
            try
            {
                var poll = await _context.Set<Poll>()
                    .Include(p => p.CreatedBy)
                    .Include(p => p.Responses)
                    .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (poll == null)
                    return null;

                // Map to view model
                var viewModel = MapPollToDetailsViewModel(poll, userId);
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving poll {PollId}", id);
                return null;
            }
        }

        // Create a new poll
        public async Task<int> CreatePollAsync(Poll poll)
        {
            try
            {
                _context.Set<Poll>().Add(poll);
                await _context.SaveChangesAsync();
                return poll.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating poll");
                return 0;
            }
        }

        // Submit a response to a poll
        // Services/PollService.cs - Updated SubmitPollResponseAsync method
        public async Task<bool> SubmitPollResponseAsync(int pollId, string userId, bool response)
        {
            try
            {
                // Important: Log the start of the operation
                _logger.LogInformation($"Starting SubmitPollResponseAsync for poll {pollId}, user {userId}, response {response}");

                // First, verify the poll exists and is active
                var poll = await _context.Polls
                    .FirstOrDefaultAsync(p => p.Id == pollId && p.IsActive);

                if (poll == null)
                {
                    _logger.LogWarning($"Poll {pollId} not found or not active");
                    return false;
                }

                _logger.LogInformation($"Found poll {pollId}: {poll.Question}");

                // Check if user has already responded to this poll
                var existingResponse = await _context.PollResponses
                    .FirstOrDefaultAsync(r => r.PollId == pollId && r.UserId == userId);

                if (existingResponse != null)
                {
                    // Update existing response
                    _logger.LogInformation($"Updating existing response for user {userId} on poll {pollId}");
                    existingResponse.Response = response;
                    existingResponse.ResponseDate = DateTime.Now;
                    _context.PollResponses.Update(existingResponse);
                }
                else
                {
                    // Create new response ONLY
                    _logger.LogInformation($"Creating new response for user {userId} on poll {pollId}");
                    var newResponse = new PollResponse
                    {
                        PollId = pollId,
                        UserId = userId,
                        Response = response,
                        ResponseDate = DateTime.Now
                    };

                    // IMPORTANT: Add ONLY to PollResponses, not to Polls
                    _context.PollResponses.Add(newResponse);
                }

                // Save changes and log the number of affected records
                var affectedRecords = await _context.SaveChangesAsync();
                _logger.LogInformation($"SaveChangesAsync completed with {affectedRecords} affected records");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting poll response for poll {PollId} and user {UserId}: {Message}",
                    pollId, userId, ex.Message);
                return false;
            }
        }

        // Close (deactivate) a poll
        public async Task<bool> ClosePollAsync(int pollId)
        {
            try
            {
                var poll = await _context.Set<Poll>().FindAsync(pollId);
                if (poll == null)
                    return false;

                poll.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing poll {PollId}", pollId);
                return false;
            }
        }

        // Delete a poll and its responses
        public async Task<bool> DeletePollAsync(int pollId)
        {
            try
            {
                // First delete all responses
                var responses = await _context.Set<PollResponse>()
                    .Where(r => r.PollId == pollId)
                    .ToListAsync();

                _context.Set<PollResponse>().RemoveRange(responses);

                // Then delete the poll
                var poll = await _context.Set<Poll>().FindAsync(pollId);
                if (poll == null)
                    return false;

                _context.Set<Poll>().Remove(poll);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting poll {PollId}", pollId);
                return false;
            }
        }

        // Helper method to map Poll entities to PollViewModel
        private List<PollViewModel> MapPollsToViewModels(List<Poll> polls, string currentUserId)
        {
            return polls.Select(p => new PollViewModel
            {
                Id = p.Id,
                Question = p.Question,
                Description = p.Description,
                CreatedDate = p.CreatedDate,
                ExpirationDate = p.ExpirationDate,
                CreatedBy = $"{p.CreatedBy.FirstName} {p.CreatedBy.LastName}",
                IsActive = p.IsActive,
                TargetAudience = p.TargetAudience,
                TotalResponses = p.Responses.Count,
                YesCount = p.Responses.Count(r => r.Response),
                NoCount = p.Responses.Count(r => !r.Response),
                UserResponse = p.Responses.FirstOrDefault(r => r.UserId == currentUserId)?.Response
            }).ToList();
        }

        // Helper method to map a Poll entity to PollDetailsViewModel
        private PollDetailsViewModel MapPollToDetailsViewModel(Poll poll, string currentUserId)
        {
            return new PollDetailsViewModel
            {
                Id = poll.Id,
                Question = poll.Question,
                Description = poll.Description,
                CreatedDate = poll.CreatedDate,
                ExpirationDate = poll.ExpirationDate,
                CreatedBy = $"{poll.CreatedBy.FirstName} {poll.CreatedBy.LastName}",
                IsActive = poll.IsActive,
                TargetAudience = poll.TargetAudience,
                TotalResponses = poll.Responses.Count,
                YesCount = poll.Responses.Count(r => r.Response),
                NoCount = poll.Responses.Count(r => !r.Response),
                UserResponse = poll.Responses.FirstOrDefault(r => r.UserId == currentUserId)?.Response,
                RecentResponses = poll.Responses
                    .OrderByDescending(r => r.ResponseDate)
                    .Take(10)
                    .Select(r => new PollResponseViewModel
                    {
                        // Updated line to handle possible null reference
                        UserName = $"{r.User?.FirstName ?? "Unknown"} {r.User?.LastName ?? "User"}",
                        Response = r.Response,
                        ResponseDate = r.ResponseDate,
                        UserRole = "User" // This would be retrieved from user roles in a real implementation
                    })
                    .ToList()
            };
        }
    }
}