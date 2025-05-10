using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GreenMeadowsPortal.Models;


namespace GreenMeadowsPortal.Data
{
    // Change from DbContext to IdentityDbContext<ApplicationUser>
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Add the missing DbSet for Announcements  
        public DbSet<AdminAnnouncement> Announcements { get; set; }
        public DbSet<AnnouncementReadReceipt> AnnouncementReadReceipts { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<DocumentModel> Documents { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; } // Add this line
                                                                   // Add the forum-related DbSets
        public DbSet<ForumTopic> ForumTopics { get; set; }
        public DbSet<ForumReply> ForumReplies { get; set; }
        public DbSet<ForumReport> ForumReports { get; set; }
        // Feedback Model - NEW
        public DbSet<Feedback> Feedbacks { get; set; }
        // Poll Models
        public DbSet<Poll> Polls { get; set; }
        public DbSet<PollResponse> PollResponses { get; set; }

        // Contact Directory Models
        public DbSet<ContactCategory> ContactCategories { get; set; }
        public DbSet<DepartmentContact> DepartmentContacts { get; set; }
        public DbSet<EmergencyContact> EmergencyContacts { get; set; }
        public DbSet<VendorContact> VendorContacts { get; set; }
        public DbSet<CommunityContact> CommunityContacts { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }

        // Don't need this line anymore since IdentityDbContext already includes Users
        // public DbSet<User> Users { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<DocumentModel>()
                 .HasOne(d => d.UploadedBy)
                 .WithMany()
                 .HasForeignKey(d => d.UploadedById)
                 .OnDelete(DeleteBehavior.Restrict);

            // Set up indexes
            builder.Entity<DocumentModel>()
                .HasIndex(d => d.Category);

            builder.Entity<DocumentModel>()
                .HasIndex(d => d.VisibleTo);
            // Contact Message Soft Delete Behavior
            builder.Entity<ContactMessage>()
                .HasQueryFilter(m =>
                    (!m.DeletedBySender && !m.DeletedByRecipient) ||
                    (m.DeletedBySender != m.DeletedByRecipient));
            // Set up indexes for Polls
            // Poll and PollResponse configuration
            builder.Entity<Poll>()
                .HasOne(p => p.CreatedBy)
                .WithMany()
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PollResponse>()
                .HasOne(r => r.Poll)
                .WithMany(p => p.Responses)
                .HasForeignKey(r => r.PollId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PollResponse>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            // Service Request relationships
            builder.Entity<ServiceRequest>()
                .HasOne(sr => sr.Requester)
                .WithMany()
                .HasForeignKey(sr => sr.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ServiceRequest>()
                .HasOne(sr => sr.AssignedTo)
                .WithMany()
                .HasForeignKey(sr => sr.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);
            // Forum-related relationships and constraints
            builder.Entity<ForumTopic>()
                .HasOne(t => t.Author)
                .WithMany()
                .HasForeignKey(t => t.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ForumReply>()
                .HasOne(r => r.Author)
                .WithMany()
                .HasForeignKey(r => r.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ForumReply>()
                .HasOne(r => r.Topic)
                .WithMany(t => t.Replies)
                .HasForeignKey(r => r.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ForumReport>()
                .HasOne(r => r.ReportedBy)
                .WithMany()
                .HasForeignKey(r => r.ReportedById)
                .OnDelete(DeleteBehavior.Restrict);
            // Feedback relationships and configuration - NEW
            builder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Feedback>()
                .HasOne(f => f.AdminUser)
                .WithMany()
                .HasForeignKey(f => f.AdminUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Set up indexes for Feedback
            builder.Entity<Feedback>()
                .HasIndex(f => f.Status);

            builder.Entity<Feedback>()
                .HasIndex(f => f.Type);

            builder.Entity<Feedback>()
                .HasIndex(f => f.Priority);

            builder.Entity<Feedback>()
                .HasIndex(f => f.SubmittedDate);

            builder.Entity<Feedback>()
                .HasIndex(f => f.UserId);

            builder.Entity<Feedback>()
                .HasIndex(f => f.AdminUserId);

        }
    }
}