using Domain.Entities; // importing entity classes
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // ASP.NET Identity EF support
using Microsoft.EntityFrameworkCore; // Entity Framework Core library

namespace Infrastructure.Data // Infrastructure layer namespace
{
    /// <summary>
    /// Database context for the application.
    /// Think of this as the "Blueprint" or "Map" of our SQL database.
    /// It tells the system what tables exist and how they connect to each other.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> // DbContext with Identity user
    {
        // The constructor sets up the connection to the SQL Server database.
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) // constructor with DB options
            : base(options) // passing options to base class
        {
        }

        // Table for policy applications (Where all customer applications are stored)
        public DbSet<PolicyApplication> PolicyApplications { get; set; }

        // Table for insurance claims (Where all accident/health claims are stored)
        public DbSet<InsuranceClaim> InsuranceClaims { get; set; }

        // Table for claim documents (Stores links to uploaded bills/photos for claims)
        public DbSet<ClaimDocument> ClaimDocuments { get; set; }

        // Table for chats (Stores the chat rooms between customers and agents)
        public DbSet<Chat> Chats { get; set; }

        // Table for chat messages (Stores every single text message in those chats)
        public DbSet<ChatMessage> ChatMessages { get; set; }

        // Table for notifications (Stores alerts shown in the user's inbox)
        public DbSet<Notification> Notifications { get; set; }

        // Table for family members included in a policy
        public DbSet<FamilyMember> FamilyMembers { get; set; }

        // Table for nominee details (who gets money if the owner dies)
        public DbSet<NomineeDetails> NomineeDetails { get; set; }

        // Table for documents uploaded during policy application
        public DbSet<ApplicationDocument> ApplicationDocuments { get; set; }

        // Table for insurance categories like "Life" or "Health"
        public DbSet<PolicyCategory> PolicyCategories { get; set; }

        // Table for tiers like "Gold", "Silver", or "Platinum"
        public DbSet<PolicyTier> PolicyTiers { get; set; }

        /// <summary>
        /// This method defines the "Rules" for the database tables (Relationships).
        /// </summary>
        protected override void OnModelCreating(ModelBuilder builder) // configuring entity relationships
        {
            base.OnModelCreating(builder); // apply identity default configuration

            // 1. Linking PolicyApplication to its Family Members
            builder.Entity<PolicyApplication>()
                .HasMany(pa => pa.FamilyMembers)
                .WithOne(fm => fm.PolicyApplication)
                .HasForeignKey(fm => fm.PolicyApplicationId);

            // 2. Linking PolicyApplication to its Nominee details
            builder.Entity<PolicyApplication>()
                .HasOne(pa => pa.Nominee)
                .WithOne(nd => nd.PolicyApplication)
                .HasForeignKey<NomineeDetails>(nd => nd.PolicyApplicationId);

            // 3. Linking PolicyApplication to its uploaded documents
            builder.Entity<PolicyApplication>()
                .HasMany(pa => pa.Documents)
                .WithOne(ad => ad.PolicyApplication)
                .HasForeignKey(ad => ad.PolicyApplicationId);

            // 4. PolicyApplication belongs to a User (Customer)
            builder.Entity<PolicyApplication>()
                .HasOne(pa => pa.User)
                .WithMany()
                .HasForeignKey(pa => pa.UserId);

            // 5. PolicyApplication is assigned to an Agent (Agent helps with the process)
            builder.Entity<PolicyApplication>()
                .HasOne(pa => pa.AssignedAgent)
                .WithMany()
                .HasForeignKey(pa => pa.AssignedAgentId);

            // 6. InsuranceClaim belongs to a Policy Application
            builder.Entity<InsuranceClaim>()
                .HasOne(c => c.Policy)
                .WithMany()
                .HasForeignKey(c => c.PolicyApplicationId)
                .OnDelete(DeleteBehavior.NoAction);

            // 7. InsuranceClaim belongs to a User (The person who got hurt)
            builder.Entity<InsuranceClaim>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 8. InsuranceClaim is reviewed by a Claims Officer
            builder.Entity<InsuranceClaim>()
                .HasOne(c => c.AssignedOfficer)
                .WithMany()
                .HasForeignKey(c => c.AssignedClaimOfficerId)
                .OnDelete(DeleteBehavior.NoAction);

            // 9. ClaimDocument belongs to an Insurance Claim
            builder.Entity<ClaimDocument>()
                .HasOne(d => d.Claim)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.ClaimId);

            // 10. Chat belongs to a specific Policy
            builder.Entity<Chat>()
                .HasOne(c => c.Policy)
                .WithMany()
                .HasForeignKey(c => c.PolicyId);

            // 11. ChatMessage belongs to a Chat session
            builder.Entity<ChatMessage>()
                .HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId);

            // 12. Notification belongs to a User
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}