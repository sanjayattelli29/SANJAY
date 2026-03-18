using Domain.Entities;
using Application.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ClaimRepository : IClaimRepository
    {
        private readonly ApplicationDbContext _context;

        public ClaimRepository(ApplicationDbContext context)
        {
            _context = context;
        }

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

        public async Task<int> GetCountByOfficerAsync(string officerId)
        {
            return await _context.InsuranceClaims.CountAsync(c => c.AssignedClaimOfficerId == officerId);
        }

        public async Task AddAsync(InsuranceClaim claim)
        {
            await _context.InsuranceClaims.AddAsync(claim);
        }

        public async Task AddDocumentAsync(ClaimDocument document)
        {
            await _context.ClaimDocuments.AddAsync(document);
        }

        public async Task UpdateAsync(InsuranceClaim claim)
        {
            _context.InsuranceClaims.Update(claim);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
