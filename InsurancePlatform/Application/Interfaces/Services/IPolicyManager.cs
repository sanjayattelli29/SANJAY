using Application.DTOs;
using Domain.Entities;
using System.Text.Json;

namespace Application.Interfaces.Services
{
    public interface IPolicyManager
    {
        Task<PolicyConfiguration> GetConfigurationAsync();
        Task<decimal> CalculatePremiumAsync(PolicyApplicationRequest request);
        Task<AuthResponseDto> ApplyForPolicyAsync(string userId, PolicyApplicationRequest request);
        Task<AuthResponseDto> SubmitPolicyDocumentsAsync(SubmitPolicyDocumentsRequest request);
        Task<IEnumerable<PolicyApplication>> GetUserPoliciesAsync(string userId);

        // Admin & Agent Management
        Task<IEnumerable<PolicyApplication>> GetAllApplicationsAsync();
        Task<IEnumerable<AgentWorkloadDto>> GetAgentsWithWorkloadAsync();
        Task<bool> AssignAgentAsync(string applicationId, string agentId);
        Task<IEnumerable<PolicyApplication>> GetAgentApplicationsAsync(string agentId);
        Task<bool> ReviewApplicationAsync(string applicationId, string status, string agentId);
        Task<bool> ProcessPaymentAsync(string applicationId, decimal amount, string transactionId);
        Task<AgentCommissionDto> GetAgentCommissionStatsAsync(string agentId);
        Task<IEnumerable<PolicyApplication>> GetAgentCustomersAsync(string agentId);
        Task<AgentAnalyticsDto> GetAgentAnalyticsAsync(string agentId);
        Task<IEnumerable<UnifiedPaymentDto>> GetUnifiedPaymentsAsync();
        Task<bool> UpdateInvoiceUrlAsync(string applicationId, string invoiceUrl);
        Task<string> UploadGeneralFileAsync(Stream fileStream, string fileName, string folder);

    }
}
