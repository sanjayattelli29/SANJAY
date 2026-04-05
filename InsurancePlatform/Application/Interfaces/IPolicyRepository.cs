using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// This interface defines how we talk to the database about Insurance Policies.
    /// It handles saving new applications, managing categories, and finding user records.
    /// </summary>
    public interface IPolicyRepository
    {
        // Find a specific policy application using its ID
        Task<PolicyApplication?> GetByIdAsync(string id, bool includeDetails = false);
        
        // Get all policies owned by a specific user
        Task<IEnumerable<PolicyApplication>> GetUserPoliciesAsync(string userId);
        
        // Get a list of every single policy application in the system
        Task<IEnumerable<PolicyApplication>> GetAllApplicationsAsync();
        
        // Get all applications being handled by a specific Agent
        Task<IEnumerable<PolicyApplication>> GetAgentApplicationsAsync(string agentId);
        
        // Check if a user already has an active policy in a certain category (like 'Health')
        Task<bool> HasActivePolicyAsync(string userId, string policyCategory);
        
        // Save a brand new policy application to the database
        Task AddAsync(PolicyApplication application);
        
        // Save details about a nominee (beneficiary) for a policy
        Task AddNomineeAsync(NomineeDetails nominee);
        
        // Save details about a family member included in a policy
        Task AddFamilyMemberAsync(FamilyMember member);
        
        // Save a link to a document (like an ID card) for a policy
        Task AddDocumentAsync(ApplicationDocument document);
        
        // Count how many policies a specific Agent is currently managing
        Task<int> GetCountByAgentAsync(string agentId);
        
        // Update the details or status of an existing policy
        Task UpdateAsync(PolicyApplication application);
        
        // Permanently save all changes to the database
        Task SaveChangesAsync();
        
        // Get a simplified list of applications for generating reports and charts
        Task<IEnumerable<PolicyApplication>> GetApplicationsForAnalyticsAsync(string? agentId = null);

        // --- Managing the Store (Categories and Prices) ---
        
        // Get all available insurance types (Life, Health, etc.) and their price levels
        Task<IEnumerable<PolicyCategory>> GetCategoriesWithTiersAsync();
        
        // Add a brand new type of insurance to the system
        Task AddCategoryAsync(PolicyCategory category);
        
        // Add a new pricing level (Tier) to an existing category
        Task AddTierAsync(PolicyTier tier);
        
        // Find a specific insurance category using its ID
        Task<PolicyCategory?> GetCategoryByIdAsync(string categoryId);
        
        // Check if a category with a certain ID already exists
        Task<bool> CategoryExistsAsync(string categoryId);
        
        // Check if a pricing tier already exists
        Task<bool> TierExistsAsync(string tierId);
    }
}
