using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPolicyRepository
    {
        Task<PolicyApplication?> GetByIdAsync(string id, bool includeDetails = false);
        Task<IEnumerable<PolicyApplication>> GetUserPoliciesAsync(string userId);
        Task<IEnumerable<PolicyApplication>> GetAllApplicationsAsync();
        Task<IEnumerable<PolicyApplication>> GetAgentApplicationsAsync(string agentId);
        Task<bool> HasActivePolicyAsync(string userId, string policyCategory);
        Task AddAsync(PolicyApplication application);
        Task AddNomineeAsync(NomineeDetails nominee);
        Task AddFamilyMemberAsync(FamilyMember member);
        Task AddDocumentAsync(ApplicationDocument document);
        Task<int> GetCountByAgentAsync(string agentId);
        Task UpdateAsync(PolicyApplication application);
        Task SaveChangesAsync();
        
        // For Analytics and Unified views
        Task<IEnumerable<PolicyApplication>> GetApplicationsForAnalyticsAsync(string? agentId = null);
    }
}
