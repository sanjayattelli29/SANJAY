using Application.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IClaimService
    {
        // Customer
        Task<object> RaiseClaimAsync(string userId, RaiseClaimRequest request);
        Task<IEnumerable<InsuranceClaim>> GetCustomerClaimsAsync(string userId);

        // Admin
        Task<IEnumerable<InsuranceClaim>> GetPendingClaimsAsync();
        Task<IEnumerable<object>> GetClaimOfficersWithWorkloadAsync();
        Task<bool> AssignClaimOfficerAsync(string claimId, string officerId);

        // Claim Officer
        Task<IEnumerable<InsuranceClaim>> GetOfficerClaimsAsync(string officerId);
        Task<bool> ReviewClaimAsync(string claimId, string status, string officerId, string remarks, decimal approvedAmount = 0);

        // Agent
        Task<IEnumerable<InsuranceClaim>> GetAgentClaimsAsync(string agentId);

        // General / Admin
        Task<IEnumerable<InsuranceClaim>> GetAllClaimsAsync();
        Task<AdminDashboardStatsDto> GetAdminStatsAsync();
    }

    public class RaiseClaimRequest
    {
        public string PolicyApplicationId { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty;
        public string IncidentLocation { get; set; } = string.Empty;
        public DateTime IncidentDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string HospitalName { get; set; } = string.Empty;
        public bool HospitalizationRequired { get; set; }
        public decimal RequestedAmount { get; set; }

        // Family Info
        public string? AffectedMemberName { get; set; }
        public string? AffectedMemberRelation { get; set; }

        public List<IFormFile>? Documents { get; set; }
    }
}
