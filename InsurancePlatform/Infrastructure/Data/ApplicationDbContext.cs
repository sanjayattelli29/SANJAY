using Domain.Entities; // importing entity classes
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // ASP.NET Identity EF support
using Microsoft.EntityFrameworkCore; // Entity Framework Core library

namespace Infrastructure.Data // Infrastructure layer namespace
{
    /// <summary>
    /// Database context for the application
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> // DbContext with Identity user
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) // constructor with DB options
            : base(options) // passing options to base class
        {
        }

        // Table for policy applications
        public DbSet<PolicyApplication> PolicyApplications { get; set; }

        // Table for insurance claims
        public DbSet<InsuranceClaim> InsuranceClaims { get; set; }

        // Table for claim documents
        public DbSet<ClaimDocument> ClaimDocuments { get; set; }

        // Table for chats
        public DbSet<Chat> Chats { get; set; }

        // Table for chat messages
        public DbSet<ChatMessage> ChatMessages { get; set; }

        // Table for notifications
        public DbSet<Notification> Notifications { get; set; }

        public DbSet<FamilyMember> FamilyMembers { get; set; }
        public DbSet<NomineeDetails> NomineeDetails { get; set; }
        public DbSet<ApplicationDocument> ApplicationDocuments { get; set; }

        public DbSet<PolicyCategory> PolicyCategories { get; set; }
        public DbSet<PolicyTier> PolicyTiers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder) // configuring entity relationships
        {
            base.OnModelCreating(builder); // apply identity default configuration

            // PolicyApplication structured data
            builder.Entity<PolicyApplication>()
                .HasMany(pa => pa.FamilyMembers)
                .WithOne(fm => fm.PolicyApplication)
                .HasForeignKey(fm => fm.PolicyApplicationId);

            builder.Entity<PolicyApplication>()
                .HasOne(pa => pa.Nominee)
                .WithOne(nd => nd.PolicyApplication)
                .HasForeignKey<NomineeDetails>(nd => nd.PolicyApplicationId);

            builder.Entity<PolicyApplication>()
                .HasMany(pa => pa.Documents)
                .WithOne(ad => ad.PolicyApplication)
                .HasForeignKey(ad => ad.PolicyApplicationId);

            // PolicyApplication linked to User using UserId foreign key
            builder.Entity<PolicyApplication>()
                .HasOne(pa => pa.User)
                .WithMany()
                .HasForeignKey(pa => pa.UserId);

            // PolicyApplication linked to AssignedAgent using AssignedAgentId
            builder.Entity<PolicyApplication>()
                .HasOne(pa => pa.AssignedAgent)
                .WithMany()
                .HasForeignKey(pa => pa.AssignedAgentId);

            // InsuranceClaim linked to PolicyApplication using PolicyApplicationId
            builder.Entity<InsuranceClaim>()
                .HasOne(c => c.Policy)
                .WithMany()
                .HasForeignKey(c => c.PolicyApplicationId)
                .OnDelete(DeleteBehavior.NoAction);

            // InsuranceClaim linked to User using UserId
            builder.Entity<InsuranceClaim>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // InsuranceClaim linked to AssignedClaimOfficer using AssignedClaimOfficerId
            builder.Entity<InsuranceClaim>()
                .HasOne(c => c.AssignedOfficer)
                .WithMany()
                .HasForeignKey(c => c.AssignedClaimOfficerId)
                .OnDelete(DeleteBehavior.NoAction);

            // ClaimDocument linked to InsuranceClaim using ClaimId
            builder.Entity<ClaimDocument>()
                .HasOne(d => d.Claim)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.ClaimId);

            // Chat linked to PolicyApplication using PolicyId
            builder.Entity<Chat>()
                .HasOne(c => c.Policy)
                .WithMany()
                .HasForeignKey(c => c.PolicyId);

            // ChatMessage linked to Chat using ChatId
            builder.Entity<ChatMessage>()
                .HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId);

            // Notification linked to User using UserId
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}