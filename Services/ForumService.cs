using GreenMeadowsPortal.Data;
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GreenMeadowsPortal.Services
{
    public interface IForumService
    {
        Task<List<ForumTopic>> GetAllTopicsAsync(int page, string sortBy, string search);
        Task<int> GetTotalTopicCountAsync(string search);
        Task<ForumTopic> GetTopicByIdAsync(int id);
        Task<ForumReply> GetReplyByIdAsync(int id);
        Task CreateTopicAsync(ForumTopic topic);
        Task AddReplyAsync(ForumReply reply);
        Task EditReplyAsync(ForumReply reply);
        Task DeleteReplyAsync(int replyId);
        Task DeleteTopicAsync(int topicId);
        Task TogglePinTopicAsync(int topicId);
        Task ToggleCloseTopicAsync(int topicId);
        Task ReportContentAsync(ForumReport report);
    }

    public class ForumService : IForumService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ForumService> _logger;

        public ForumService(AppDbContext context, ILogger<ForumService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ForumTopic>> GetAllTopicsAsync(int page, string sortBy, string search)
        {
            try
            {
                var query = _context.ForumTopics
                    .Include(t => t.Author)
                    .Include(t => t.Replies)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(t => t.Title.Contains(search) || t.Content.Contains(search));
                }

                query = sortBy switch
                {
                    "activity" => query.OrderByDescending(t => t.LastActivityDate),
                    "views" => query.OrderByDescending(t => t.ViewCount),
                    "replies" => query.OrderByDescending(t => t.ReplyCount),
                    _ => query.OrderByDescending(t => t.CreatedDate)
                };

                return await query
                    .Skip((page - 1) * 10)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting forum topics");
                throw;
            }
        }

        public async Task<int> GetTotalTopicCountAsync(string search)
        {
            try
            {
                var query = _context.ForumTopics.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(t => t.Title.Contains(search) || t.Content.Contains(search));
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting total topic count");
                throw;
            }
        }

        public async Task<ForumTopic> GetTopicByIdAsync(int id)
        {
            try
            {
                var topic = await _context.ForumTopics
                    .Include(t => t.Author)
                    .Include(t => t.Replies)
                        .ThenInclude(r => r.Author)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (topic == null)
                {
                    throw new KeyNotFoundException($"Topic with ID {id} not found");
                }

                topic.ViewCount++;
                await _context.SaveChangesAsync();

                return topic;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting topic {TopicId}", id);
                throw;
            }
        }

        public async Task<ForumReply> GetReplyByIdAsync(int id)
        {
            try
            {
                var reply = await _context.ForumReplies
                    .Include(r => r.Author)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reply == null)
                {
                    throw new KeyNotFoundException($"Reply with ID {id} not found");
                }

                return reply;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting reply {ReplyId}", id);
                throw;
            }
        }

        public async Task CreateTopicAsync(ForumTopic topic)
        {
            try
            {
                if (topic == null)
                {
                    throw new ArgumentNullException(nameof(topic));
                }

                topic.CreatedDate = DateTime.Now;
                topic.LastActivityDate = DateTime.Now;
                topic.ViewCount = 0;
                topic.ReplyCount = 0;
                topic.IsPinned = false;
                topic.IsClosed = false;

                _context.ForumTopics.Add(topic);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating topic");
                throw;
            }
        }

        public async Task AddReplyAsync(ForumReply reply)
        {
            try
            {
                if (reply == null)
                {
                    throw new ArgumentNullException(nameof(reply));
                }

                var topic = await _context.ForumTopics.FindAsync(reply.TopicId);
                if (topic == null)
                {
                    throw new KeyNotFoundException($"Topic with ID {reply.TopicId} not found");
                }

                if (topic.IsClosed)
                {
                    throw new InvalidOperationException("Cannot add reply to a closed topic");
                }

                reply.CreatedDate = DateTime.Now;
                topic.LastActivityDate = DateTime.Now;
                topic.ReplyCount++;

                _context.ForumReplies.Add(reply);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding reply to topic {TopicId}", reply?.TopicId);
                throw;
            }
        }

        public async Task EditReplyAsync(ForumReply reply)
        {
            try
            {
                if (reply == null)
                {
                    throw new ArgumentNullException(nameof(reply));
                }

                var existingReply = await _context.ForumReplies.FindAsync(reply.Id);
                if (existingReply == null)
                {
                    throw new KeyNotFoundException($"Reply with ID {reply.Id} not found");
                }

                existingReply.Content = reply.Content;
                existingReply.EditedDate = DateTime.Now;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while editing reply {ReplyId}", reply?.Id);
                throw;
            }
        }

        public async Task DeleteReplyAsync(int replyId)
        {
            try
            {
                var reply = await _context.ForumReplies
                    .Include(r => r.Topic)
                    .FirstOrDefaultAsync(r => r.Id == replyId);

                if (reply == null)
                {
                    throw new KeyNotFoundException($"Reply with ID {replyId} not found");
                }

                var topic = reply.Topic;
                topic.ReplyCount--;
                topic.LastActivityDate = DateTime.Now;

                _context.ForumReplies.Remove(reply);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting reply {ReplyId}", replyId);
                throw;
            }
        }

        public async Task DeleteTopicAsync(int topicId)
        {
            try
            {
                // Remove all related ForumReports first
                var reports = _context.ForumReports.Where(r => r.TopicId == topicId);
                _context.ForumReports.RemoveRange(reports);
                await _context.SaveChangesAsync();

                // Remove all related ForumReplies
                var replies = _context.ForumReplies.Where(r => r.TopicId == topicId);
                _context.ForumReplies.RemoveRange(replies);
                await _context.SaveChangesAsync();

                // Remove the topic
                var topic = await _context.ForumTopics.FirstOrDefaultAsync(t => t.Id == topicId);
                if (topic == null)
                {
                    throw new KeyNotFoundException($"Topic with ID {topicId} not found");
                }
                _context.ForumTopics.Remove(topic);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting topic {TopicId}", topicId);
                throw;
            }
        }

        public async Task TogglePinTopicAsync(int topicId)
        {
            try
            {
                var topic = await _context.ForumTopics.FindAsync(topicId);
                if (topic == null)
                {
                    throw new KeyNotFoundException($"Topic with ID {topicId} not found");
                }

                topic.IsPinned = !topic.IsPinned;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while toggling pin status for topic {TopicId}", topicId);
                throw;
            }
        }

        public async Task ToggleCloseTopicAsync(int topicId)
        {
            try
            {
                var topic = await _context.ForumTopics.FindAsync(topicId);
                if (topic == null)
                {
                    throw new KeyNotFoundException($"Topic with ID {topicId} not found");
                }

                topic.IsClosed = !topic.IsClosed;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while toggling close status for topic {TopicId}", topicId);
                throw;
            }
        }

        public async Task ReportContentAsync(ForumReport report)
        {
            try
            {
                if (report == null)
                {
                    throw new ArgumentNullException(nameof(report));
                }

                if (report.TopicId == null && report.ReplyId == null)
                {
                    throw new ArgumentException("Either TopicId or ReplyId must be provided");
                }

                report.ReportDate = DateTime.Now;
                report.IsResolved = false;

                _context.ForumReports.Add(report);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while reporting content");
                throw;
            }
        }
    }
}