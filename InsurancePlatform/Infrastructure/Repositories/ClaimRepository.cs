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
    /// This class manages all the "Storage and Retrieval" for Insurance Claims.
    /// It helps agents find pending claims and helps customers see their submissons.
    /// </summary>
    public class ClaimRepository : IClaimRepository
    {
        private readonly ApplicationDbContext _context;

        // Constructor to setup the database access.
        public ClaimRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get all details for a single claim using its unique ID.
        public async Task<InsuranceClaim?> GetByIdAsync(string id)
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)
                .Include(c => c.Policy)
                    .ThenInclude(p => p.AssignedAgent)
                .Include(c => c.Documents)
                .Include(c => c.AssignedOfficer)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // Find all claims submitted by a specific Customer.
        public async Task<IEnumerable<InsuranceClaim>> GetByUserIdAsync(string userId)
        {
            return await _context.InsuranceClaims
                .Include(c => c.Policy)
                .Include(c => c.Documents)
                .Include(c => c.AssignedOfficer)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        // Find all claims that are still "Pending" and need an officer's review.
        public async Task<IEnumerable<InsuranceClaim>> GetPendingClaimsAsync()
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)
                .Include(c => c.Policy)
                .Include(c => c.Documents)
                .Where(c => c.Status == "Pending")
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        // Find all claims assigned to a specific Claims Officer.
        public async Task<IEnumerable<InsuranceClaim>> GetByOfficerIdAsync(string officerId)
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)
                .Include(c => c.Policy)
                    .ThenInclude(p => p.AssignedAgent)
                .Include(c => c.Documents)
                .Where(c => c.AssignedClaimOfficerId == officerId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        // Find all claims for policies managed by a specific Agent.
        public async Task<IEnumerable<InsuranceClaim>> GetByAgentIdAsync(string agentId)
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)
                .Include(c => c.Policy)
                    .ThenInclude(p => p.AssignedAgent)
                .Include(c => c.Documents)
                .Include(c => c.AssignedOfficer)
                .Where(c => c.Policy.AssignedAgentId == agentId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        // Get a complete list of every single claim in the entire system.
        public async Task<IEnumerable<InsuranceClaim>> GetAllClaimsAsync()
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)
                .Include(c => c.Policy)
                    .ThenInclude(p => p.AssignedAgent)
                .Include(c => c.Documents)
                .Include(c => c.AssignedOfficer)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        // Find the claim associated with a specific policy.
        public async Task<InsuranceClaim?> GetByPolicyIdAsync(string policyId)
        {
            return await _context.InsuranceClaims
                .Include(c => c.User)
                .Include(c => c.Policy)
                    .ThenInclude(p => p.AssignedAgent)
                .Include(c => c.Documents)
                .Include(c => c.AssignedOfficer)
                .FirstOrDefaultAsync(c => c.PolicyApplicationId == policyId);
        }

        // Count how many claims a specific officer is currently handling.
        public async Task<int> GetCountByOfficerAsync(string officerId)
        {
            return await _context.InsuranceClaims.CountAsync(c => c.AssignedClaimOfficerId == officerId);
        }

        // Add a brand new claim to the database.
        public async Task AddAsync(InsuranceClaim claim)
        {
            await _context.InsuranceClaims.AddAsync(claim);
        }

        // Save an uploaded document (like a hospital bill) related to a claim.
        public async Task AddDocumentAsync(ClaimDocument document)
        {
            await _context.ClaimDocuments.AddAsync(document);
        }

        // Update an existing claim (e.g., when it gets approved or rejected).
        public async Task UpdateAsync(InsuranceClaim claim)
        {
            _context.InsuranceClaims.Update(claim);
            await Task.CompletedTask;
        }

        // Save all changes to the SQL Server database.
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
