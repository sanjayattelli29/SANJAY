using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IClaimRepository
    {
        Task<InsuranceClaim?> GetByIdAsync(string id);
        Task<IEnumerable<InsuranceClaim>> GetByUserIdAsync(string userId);
        Task<IEnumerable<InsuranceClaim>> GetPendingClaimsAsync();
        Task<IEnumerable<InsuranceClaim>> GetByOfficerIdAsync(string officerId);
        Task<IEnumerable<InsuranceClaim>> GetByAgentIdAsync(string agentId);
        Task<IEnumerable<InsuranceClaim>> GetAllClaimsAsync();
        Task<InsuranceClaim?> GetByPolicyIdAsync(string policyId);
        
        Task<int> GetCountByOfficerAsync(string officerId);
        
        Task AddAsync(InsuranceClaim claim);
        Task AddDocumentAsync(ClaimDocument document);
        Task UpdateAsync(InsuranceClaim claim);
        Task SaveChangesAsync();
    }
}
