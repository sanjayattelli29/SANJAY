using Microsoft.EntityFrameworkCore;
using InsurancePlatform.Models;

namespace InsurancePlatform.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<CustomerProfile> CustomerProfiles { get; set; }
        public DbSet<AgentProfile> AgentProfiles { get; set; }
        public DbSet<ClaimsOfficerProfile> ClaimsOfficerProfiles { get; set; }
        public DbSet<Policy> Policies { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<ClaimDocument> ClaimDocuments { get; set; }
        public DbSet<CommissionLedger> CommissionLedgers { get; set; }
        public DbSet<RiskAssessment> RiskAssessments { get; set; }
        public DbSet<AssignmentTracker> AssignmentTrackers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // UNIQUE CONSTRAINTS
            // =========================

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<Policy>()
                .HasIndex(p => p.PolicyNumber)
                .IsUnique();

            modelBuilder.Entity<AgentProfile>()
                .HasIndex(a => a.EmployeeCode)
                .IsUnique();


            // =========================
            // ONE-TO-ONE RELATIONSHIPS
            // =========================

            modelBuilder.Entity<User>()
                .HasOne(u => u.CustomerProfile)
                .WithOne(c => c.User)
                .HasForeignKey<CustomerProfile>(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.AgentProfile)
                .WithOne(a => a.User)
                .HasForeignKey<AgentProfile>(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.ClaimsOfficerProfile)
                .WithOne(c => c.User)
                .HasForeignKey<ClaimsOfficerProfile>(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // POLICY RELATIONSHIPS
            // =========================

            modelBuilder.Entity<Policy>()
                .HasOne(p => p.Customer)
                .WithMany()
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Policy>()
                .HasOne(p => p.AssignedAgent)
                .WithMany()
                .HasForeignKey(p => p.AssignedAgentId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // LEAD RELATIONSHIPS (FIXED)
            // =========================

            modelBuilder.Entity<Lead>()
                .HasOne(l => l.Customer)
                .WithMany()
                .HasForeignKey(l => l.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lead>()
                .HasOne(l => l.AssignedAgent)
                .WithMany()
                .HasForeignKey(l => l.AssignedAgentId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // CLAIM RELATIONSHIPS
            // =========================

            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Policy)
                .WithMany()
                .HasForeignKey(c => c.PolicyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Claim>()
                .HasOne(c => c.AssignedClaimsOfficer)
                .WithMany()
                .HasForeignKey(c => c.AssignedClaimsOfficerId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // CLAIM DOCUMENT RELATIONSHIP
            // =========================

            modelBuilder.Entity<ClaimDocument>()
                .HasOne(cd => cd.Claim)
                .WithMany()
                .HasForeignKey(cd => cd.ClaimId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // COMMISSION RELATIONSHIPS
            // =========================

            modelBuilder.Entity<CommissionLedger>()
                .HasOne(c => c.Agent)
                .WithMany()
                .HasForeignKey(c => c.AgentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommissionLedger>()
                .HasOne(c => c.Policy)
                .WithMany()
                .HasForeignKey(c => c.PolicyId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================
            // DECIMAL PRECISION
            // =========================

            modelBuilder.Entity<Policy>()
                .Property(p => p.PremiumAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Policy>()
                .Property(p => p.SumInsured)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CustomerProfile>()
                .Property(c => c.AnnualIncome)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CommissionLedger>()
                .Property(c => c.CommissionAmount)
                .HasColumnType("decimal(18,2)");

            // RiskAssessment decimal precision fix

            modelBuilder.Entity<RiskAssessment>()
                .Property(r => r.AgeScore)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RiskAssessment>()
                .Property(r => r.OccupationScore)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RiskAssessment>()
                .Property(r => r.BehaviorScore)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RiskAssessment>()
                .Property(r => r.TravelExposureScore)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RiskAssessment>()
                .Property(r => r.LocationScore)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RiskAssessment>()
                .Property(r => r.MedicalScore)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RiskAssessment>()
                .Property(r => r.TotalRiskScore)
                .HasColumnType("decimal(18,2)");
        }
    }
}