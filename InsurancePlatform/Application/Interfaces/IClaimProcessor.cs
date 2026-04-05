using Application.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// This interface defines the main "Brain" for handling insurance claims.
    /// It contains logic for Customers, Admins, agents, and Claim Officers.
    /// </summary>
    public interface IClaimProcessor
    {
        // --- For Customers ---
        
        // Allows a customer to start a new claim (ask for money) after an incident
        Task<AuthResponseDto> RaiseClaimAsync(string userId, RaiseClaimRequest request);
        
        // Shows a customer a list of all claims they have filed
        Task<IEnumerable<InsuranceClaim>> GetCustomerClaimsAsync(string userId);

        // --- For Admins ---
        
        // Shows a list of all claims that are waiting for someone to look at them
        Task<IEnumerable<InsuranceClaim>> GetPendingClaimsAsync();
        
        // Shows how busy each Claim Officer is (so the admin can pick the least busy one)
        Task<IEnumerable<ClaimOfficerWorkloadDto>> GetClaimOfficersWithWorkloadAsync();
        
        // Tells the system which Officer should handle which claim
        Task<bool> AssignClaimOfficerAsync(string claimId, string officerId);

        // --- For Claim Officers ---
        
        // Shows an officer all the claims that have been assigned to them
        Task<IEnumerable<InsuranceClaim>> GetOfficerClaimsAsync(string officerId);
        
        // Allows an officer to Approve or Reject a claim after reviewing the proof
        Task<bool> ReviewClaimAsync(string claimId, string status, string officerId, string remarks, decimal approvedAmount = 0);

        // --- For Insurance Agents ---
        
        // Shows an agent all the claims filed by their customers
        Task<IEnumerable<InsuranceClaim>> GetAgentClaimsAsync(string agentId);

        // --- General Tools ---
        
        // Get every single claim ever filed in the system
        Task<IEnumerable<InsuranceClaim>> GetAllClaimsAsync();
        
        // Get the big numbers (Total Claims, Money Paid, etc.) for the Admin dashboard
        Task<AdminDashboardStatsDto> GetAdminStatsAsync();
        
        // Find a specific claim using its Policy ID
        Task<InsuranceClaim?> GetClaimByPolicyIdAsync(string policyId);
        
        // Save the web link to an AI-generated analysis report for a claim
        Task UpdateAnalysisUrlAsync(string claimId, string analysisUrl);
    }
}
