using Application.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic; // standard list
using System.Threading.Tasks; // async tasks

namespace Application.Interfaces // folder path
{
    // this interface is for everything about insurance claims
    public interface IClaimService // claim management interface
    {
        // customer wants money for an issue
        Task<object> RaiseClaimAsync(string userId, RaiseClaimRequest request);
        // get a list of all claims for one customer
        Task<IEnumerable<InsuranceClaim>> GetCustomerClaimsAsync(string userId);

        // admin functions
        // list all claims that nobody is checking yet
        Task<IEnumerable<InsuranceClaim>> GetPendingClaimsAsync(); // new claims list
        // see which officers are busy and who is free
        Task<IEnumerable<object>> GetClaimOfficersWithWorkloadAsync();
        // give a claim to a specific officer to check
        Task<bool> AssignClaimOfficerAsync(string claimId, string officerId); // admin task

        // officer functions
        // list of claims given to this officer
        Task<IEnumerable<InsuranceClaim>> GetOfficerClaimsAsync(string officerId); // officer's work
        // officer gives decision on a claim
        Task<bool> ReviewClaimAsync(string claimId, string status, string officerId, string remarks, decimal approvedAmount = 0);

        // agent functions
        // list of claims for policies sold by this agent
        Task<IEnumerable<InsuranceClaim>> GetAgentClaimsAsync(string agentId); // agent's customer claims

        // overall functions
        // list every claim in the system
        Task<IEnumerable<InsuranceClaim>> GetAllClaimsAsync();
        // get the numbers for admin dashboard
        Task<AdminDashboardStatsDto> GetAdminStatsAsync();
        // find a claim by policy id
        Task<InsuranceClaim?> GetClaimByPolicyIdAsync(string policyId); // find by policy
    }
}
// claim service contract end
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
