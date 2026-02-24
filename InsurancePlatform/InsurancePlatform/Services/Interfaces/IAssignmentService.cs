using InsurancePlatform.Models;

namespace InsurancePlatform.Services.Interfaces
{
    public interface IAssignmentService
    {
        Task<Guid> GetLeastLoadedAgentIdAsync();
        Task<Guid> GetLeastLoadedAgentByLeadCountAsync();
        Task<Guid> GetLeastLoadedClaimsOfficerIdAsync();
    }
}
