using Application.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IClaimProcessor
    {
        // Customer
        Task<AuthResponseDto> RaiseClaimAsync(string userId, RaiseClaimRequest request);
        Task<IEnumerable<InsuranceClaim>> GetCustomerClaimsAsync(string userId);

        // Admin
        Task<IEnumerable<InsuranceClaim>> GetPendingClaimsAsync();
        Task<IEnumerable<ClaimOfficerWorkloadDto>> GetClaimOfficersWithWorkloadAsync();
        Task<bool> AssignClaimOfficerAsync(string claimId, string officerId);

        // Claim Officer
        Task<IEnumerable<InsuranceClaim>> GetOfficerClaimsAsync(string officerId);
        Task<bool> ReviewClaimAsync(string claimId, string status, string officerId, string remarks, decimal approvedAmount = 0);

        // Agent
        Task<IEnumerable<InsuranceClaim>> GetAgentClaimsAsync(string agentId);

        // General / Admin
        Task<IEnumerable<InsuranceClaim>> GetAllClaimsAsync();
        Task<AdminDashboardStatsDto> GetAdminStatsAsync();
        Task<InsuranceClaim?> GetClaimByPolicyIdAsync(string policyId);
    }
}
