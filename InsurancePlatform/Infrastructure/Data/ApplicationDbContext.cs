using Domain.Entities; // for table models
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // for identity tables
using Microsoft.EntityFrameworkCore; // for database tools

namespace Infrastructure.Data
{
    /// <summary>
    /// Database context for the application, inheriting from IdentityDbContext to support ASP.NET Core Identity.
    /// </summary>
namespace Infrastructure.Data
{
    // this class is the bridge between our code and the sql database
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) // constructor
            : base(options) // pass settings to base class
        {
        }

        // table for insurance applications
        public DbSet<PolicyApplication> PolicyApplications { get; set; }
        // table for claims filed
        public DbSet<InsuranceClaim> InsuranceClaims { get; set; }
        // table for uploaded documents
        public DbSet<ClaimDocument> ClaimDocuments { get; set; }
        // table for chat sessions
        public DbSet<Chat> Chats { get; set; }
        // table for separate chat messages
        public DbSet<ChatMessage> ChatMessages { get; set; } // list of messages

        protected override void OnModelCreating(ModelBuilder builder) // configure relationships
        {
            base.OnModelCreating(builder); // call identity logic
            
            builder.Entity<PolicyApplication>()
                .HasOne(pa => pa.User)
                .WithMany()
                .HasForeignKey(pa => pa.UserId);

            builder.Entity<PolicyApplication>()
                .HasOne(pa => pa.AssignedAgent)
                .WithMany()
                .HasForeignKey(pa => pa.AssignedAgentId);

            builder.Entity<InsuranceClaim>()
                .HasOne(c => c.Policy)
                .WithMany()
                .HasForeignKey(c => c.PolicyApplicationId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<InsuranceClaim>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InsuranceClaim>()
                .HasOne(c => c.AssignedOfficer)
                .WithMany()
                .HasForeignKey(c => c.AssignedClaimOfficerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ClaimDocument>()
                .HasOne(d => d.Claim)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.ClaimId);

            builder.Entity<Chat>()
                .HasOne(c => c.Policy)
                .WithMany()
                .HasForeignKey(c => c.PolicyId);

            builder.Entity<ChatMessage>()
                .HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId);
        }
    }
}
// db context definition end
