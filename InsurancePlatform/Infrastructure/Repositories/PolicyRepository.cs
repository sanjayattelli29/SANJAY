using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Infrastructure.Repositories
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly ApplicationDbContext _context;

        public PolicyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PolicyApplication?> GetByIdAsync(string id, bool includeDetails = false)
        {
            var query = _context.PolicyApplications.AsQueryable();
            
            if (includeDetails)
            {
                query = query
                    .Include(pa => pa.User)
                    .Include(pa => pa.AssignedAgent)
                    .Include(pa => pa.FamilyMembers)
                    .Include(pa => pa.Nominee)
                    .Include(pa => pa.Documents);
            }

            return await query.FirstOrDefaultAsync(pa => pa.Id == id);
        }

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

        public async Task<bool> HasActivePolicyAsync(string userId, string policyCategory)
        {
            return await _context.PolicyApplications
                .AnyAsync(pa => pa.UserId == userId && 
                                pa.PolicyCategory == policyCategory && 
                                pa.Status == "Active");
        }

        public async Task AddAsync(PolicyApplication application)
        {
            await _context.PolicyApplications.AddAsync(application);
        }

        public async Task AddNomineeAsync(NomineeDetails nominee)
        {
            await _context.NomineeDetails.AddAsync(nominee);
        }

        public async Task AddFamilyMemberAsync(FamilyMember member)
        {
            await _context.FamilyMembers.AddAsync(member);
        }

        public async Task AddDocumentAsync(ApplicationDocument document)
        {
            await _context.ApplicationDocuments.AddAsync(document);
        }

        public async Task<int> GetCountByAgentAsync(string agentId)
        {
            return await _context.PolicyApplications.CountAsync(pa => pa.AssignedAgentId == agentId);
        }

        public async Task UpdateAsync(PolicyApplication application)
        {
            _context.PolicyApplications.Update(application);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

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

        public async Task<IEnumerable<PolicyCategory>> GetCategoriesWithTiersAsync()
        {
            return await _context.PolicyCategories.Include(c => c.Tiers).ToListAsync();
        }

        public async Task AddCategoryAsync(PolicyCategory category)
        {
            await _context.PolicyCategories.AddAsync(category);
        }

        public async Task AddTierAsync(PolicyTier tier)
        {
            await _context.PolicyTiers.AddAsync(tier);
        }

        public async Task<PolicyCategory?> GetCategoryByIdAsync(string categoryId)
        {
            return await _context.PolicyCategories.FindAsync(categoryId);
        }

        public async Task<bool> CategoryExistsAsync(string categoryId)
        {
            return await _context.PolicyCategories.AnyAsync(c => c.CategoryId == categoryId);
        }

        public async Task<bool> TierExistsAsync(string tierId)
        {
            return await _context.PolicyTiers.AnyAsync(t => t.TierId == tierId);
        }
    }
}
