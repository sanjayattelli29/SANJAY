using Application.DTOs;
using Domain.Entities;
using System.Text.Json;

namespace Application.Interfaces
{
    /// <summary>
    /// This interface is the main "Manager" for all things related to Insurance Policies.
    /// It handles everything from pricing to payments and performance reports.
    /// </summary>
    public interface IPolicyManager
    {
        // Get the list of all available policy categories and pricing tiers
        Task<PolicyConfiguration> GetConfigurationAsync();
        
        // Calculate the price (Premium) for a policy based on the user's data
        Task<decimal> CalculatePremiumAsync(PolicyApplicationRequest request);
        
        // Start a brand new policy application for a customer
        Task<AuthResponseDto> ApplyForPolicyAsync(string userId, PolicyApplicationRequest request);
        
        // Upload the required identity and income documents for a policy
        Task<AuthResponseDto> SubmitPolicyDocumentsAsync(SubmitPolicyDocumentsRequest request);
        
        // Get all policies that a specific user has bought or applied for
        Task<IEnumerable<PolicyApplication>> GetUserPoliciesAsync(string userId);

        // --- For Admins and Staff ---
        
        // Create a whole new type of insurance (e.g., 'Travel Insurance')
        Task<bool> CreatePolicyCategoryAsync(PolicyCategory category);
        
        // Add a new pricing level (e.g., 'Platinum Tier') to an existing category
        Task<bool> AddPolicyTierAsync(string categoryId, PolicyTier tier);

        // Get all applications in the system that need processing
        Task<IEnumerable<PolicyApplication>> GetAllApplicationsAsync();
        
        // See which agents are busy and which can take more work
        Task<IEnumerable<AgentWorkloadDto>> GetAgentsWithWorkloadAsync();
        
        // Assign a specific agent to help a customer with their application
        Task<bool> AssignAgentAsync(string applicationId, string agentId);
        
        // Get all applications currently being handled by a specific agent
        Task<IEnumerable<PolicyApplication>> GetAgentApplicationsAsync(string agentId);
        
        // Allow an agent to Approve or Reject a policy application
        Task<bool> ReviewApplicationAsync(string applicationId, string status, string agentId);
        
        // Record a payment made by a customer for their policy
        Task<bool> ProcessPaymentAsync(string applicationId, decimal amount, string transactionId);
        
        // Get statistics on how much commission an agent has earned
        Task<AgentCommissionDto> GetAgentCommissionStatsAsync(string agentId);
        
        // Get a list of all customers who bought policies from a specific agent
        Task<IEnumerable<PolicyApplication>> GetAgentCustomersAsync(string agentId);
        
        // Get a detailed "Performance Report" for an agent
        Task<AgentAnalyticsDto> GetAgentAnalyticsAsync(string agentId);
        
        // Get a list of all money transactions in the system
        Task<IEnumerable<UnifiedPaymentDto>> GetUnifiedPaymentsAsync();
        
        // Save the web link to a payment receipt (Invoice)
        Task<bool> UpdateInvoiceUrlAsync(string applicationId, string invoiceUrl);
        
        // Save the web link to an AI-generated analysis report
        Task<bool> UpdateAnalysisUrlAsync(string applicationId, string analysisUrl);
        
        // A general tool to upload any file to the storage service
        Task<string> UploadGeneralFileAsync(Stream fileStream, string fileName, string folder);
    }
}
