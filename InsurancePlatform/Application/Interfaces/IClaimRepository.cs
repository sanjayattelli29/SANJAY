using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// This interface defines how we talk to the database about insurance claims.
    /// It handles saving, finding, and updating claim records.
    /// </summary>
    public interface IClaimRepository
    {
        // Find a claim using its internal unique ID
        Task<InsuranceClaim?> GetByIdAsync(string id);
        
        // Get all claims filed by a specific user
        Task<IEnumerable<InsuranceClaim>> GetByUserIdAsync(string userId);
        
        // Get a list of claims that haven't been reviewed yet
        Task<IEnumerable<InsuranceClaim>> GetPendingClaimsAsync();
        
        // Get all claims assigned to a specific Claim Officer
        Task<IEnumerable<InsuranceClaim>> GetByOfficerIdAsync(string officerId);
        
        // Get all claims filed by customers of a specific Agent
        Task<IEnumerable<InsuranceClaim>> GetByAgentIdAsync(string agentId);
        
        // Get every single claim in the entire system
        Task<IEnumerable<InsuranceClaim>> GetAllClaimsAsync();
        
        // Find a claim linked to a specific policy
        Task<InsuranceClaim?> GetByPolicyIdAsync(string policyId);
        
        // Count how many claims a specific Officer is currently working on
        Task<int> GetCountByOfficerAsync(string officerId);
        
        // Add a brand new claim to the database
        Task AddAsync(InsuranceClaim claim);
        
        // Save a document (like a bill or report) linked to a claim
        Task AddDocumentAsync(ClaimDocument document);
        
        // Update coordinates or details of an existing claim
        Task UpdateAsync(InsuranceClaim claim);
        
        // Save all changes to the database permanently
        Task SaveChangesAsync();
    }
}
