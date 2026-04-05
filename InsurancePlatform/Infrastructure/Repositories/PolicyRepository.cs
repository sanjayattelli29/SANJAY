using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// This class handles all the "Data Work" for Insurance Policies.
    /// It saves new applications, finds existing ones, and manages policy categories.
    /// </summary>
    public class PolicyRepository : IPolicyRepository
    {
        private readonly ApplicationDbContext _context;

        // Constructor sets up the database connection.
        public PolicyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Find a specific policy application. "includeDetails" loads the user, family, and documents too.
        public async Task<PolicyApplication?> GetByIdAsync(string id, bool includeDetails = false)
        {
            var query = _context.PolicyApplications.AsQueryable();
            
            if (includeDetails)
            {
                // Join related tables so we have all the info in one package.
                query = query
                    .Include(pa => pa.User)
                    .Include(pa => pa.AssignedAgent)
                    .Include(pa => pa.FamilyMembers)
                    .Include(pa => pa.Nominee)
                    .Include(pa => pa.Documents);
            }

            return await query.FirstOrDefaultAsync(pa => pa.Id == id);
        }

        // Get all policy applications submitted by a specific Customer.
        public async Task<IEnumerable<PolicyApplication>> GetUserPoliciesAsync(string userId)
        {
            return await _context.PolicyApplications
                .Include(pa => pa.FamilyMembers)
                .Include(pa => pa.Nominee)
                .Include(pa => pa.Documents)
                .Where(pa => pa.UserId == userId)
                .OrderByDescending(pa => pa.SubmissionDate)
                .ToListAsync();
        }

        // Get a list of EVERY policy application ever submitted.
        public async Task<IEnumerable<PolicyApplication>> GetAllApplicationsAsync()
        {
            return await _context.PolicyApplications
                .Include(pa => pa.User)
                .Include(pa => pa.AssignedAgent)
                .Include(pa => pa.FamilyMembers)
                .Include(pa => pa.Nominee)
                .Include(pa => pa.Documents)
                .OrderByDescending(pa => pa.SubmissionDate)
                .ToListAsync();
        }

        // Get all policy applications that are assigned to a specific Agent.
        public async Task<IEnumerable<PolicyApplication>> GetAgentApplicationsAsync(string agentId)
        {
            return await _context.PolicyApplications
                .Include(pa => pa.User)
                .Include(pa => pa.FamilyMembers)
                .Include(pa => pa.Nominee)
                .Include(pa => pa.Documents)
                .Where(pa => pa.AssignedAgentId == agentId)
                .OrderByDescending(pa => pa.SubmissionDate)
                .ToListAsync();
        }

        // Check if a user already has a specific type of active policy (e.g., they already have Life Insurance).
        public async Task<bool> HasActivePolicyAsync(string userId, string policyCategory)
        {
            return await _context.PolicyApplications
                .AnyAsync(pa => pa.UserId == userId && 
                                pa.PolicyCategory == policyCategory && 
                                pa.Status == "Active");
        }

        // Save a brand new application to the database.
        public async Task AddAsync(PolicyApplication application)
        {
            await _context.PolicyApplications.AddAsync(application);
        }

        // Save nominee details for an application.
        public async Task AddNomineeAsync(NomineeDetails nominee)
        {
            await _context.NomineeDetails.AddAsync(nominee);
        }

        // Add a family member to an existing policy.
        public async Task AddFamilyMemberAsync(FamilyMember member)
        {
            await _context.FamilyMembers.AddAsync(member);
        }

        // Save an uploaded document related to a policy.
        public async Task AddDocumentAsync(ApplicationDocument document)
        {
            await _context.ApplicationDocuments.AddAsync(document);
        }

        // Count how many applications a specific agent is processing.
        public async Task<int> GetCountByAgentAsync(string agentId)
        {
            return await _context.PolicyApplications.CountAsync(pa => pa.AssignedAgentId == agentId);
        }

        // Update information for an existing application (e.g., set status to "Approved").
        public async Task UpdateAsync(PolicyApplication application)
        {
            _context.PolicyApplications.Update(application);
            await Task.CompletedTask;
        }

        // Save all changes to the SQL Server database.
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // Get detailed data for charts and analytics.
        public async Task<IEnumerable<PolicyApplication>> GetApplicationsForAnalyticsAsync(string? agentId = null)
        {
            var query = _context.PolicyApplications
                .Include(pa => pa.User)
                .Include(pa => pa.AssignedAgent)
                .Include(pa => pa.FamilyMembers)
                .Include(pa => pa.Nominee)
                .Include(pa => pa.Documents)
                .AsQueryable();

            if (!string.IsNullOrEmpty(agentId))
            {
                query = query.Where(pa => pa.AssignedAgentId == agentId);
            }

            return await query.ToListAsync();
        }

        // Get all categories (like Health, Life) and the plans inside them.
        public async Task<IEnumerable<PolicyCategory>> GetCategoriesWithTiersAsync()
        {
            return await _context.PolicyCategories.Include(c => c.Tiers).ToListAsync();
        }

        // Create a new insurance category.
        public async Task AddCategoryAsync(PolicyCategory category)
        {
            await _context.PolicyCategories.AddAsync(category);
        }

        // Add a new tier (like "Gold Plan") to a category.
        public async Task AddTierAsync(PolicyTier tier)
        {
            await _context.PolicyTiers.AddAsync(tier);
        }

        // Find a specific category by its ID.
        public async Task<PolicyCategory?> GetCategoryByIdAsync(string categoryId)
        {
            return await _context.PolicyCategories.FindAsync(categoryId);
        }

        // Check if a category already exists.
        public async Task<bool> CategoryExistsAsync(string categoryId)
        {
            return await _context.PolicyCategories.AnyAsync(c => c.CategoryId == categoryId);
        }

        // Check if a specific tier (e.g., Gold) already exists.
        public async Task<bool> TierExistsAsync(string tierId)
        {
            return await _context.PolicyTiers.AnyAsync(t => t.TierId == tierId);
        }
    }
}
