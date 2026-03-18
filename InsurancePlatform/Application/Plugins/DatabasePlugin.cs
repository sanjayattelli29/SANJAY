using Microsoft.SemanticKernel;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.Text.Json;

namespace Application.Plugins
{
    public class DatabasePlugin
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public DatabasePlugin(
            IPolicyRepository policyRepository,
            IClaimRepository claimRepository,
            UserManager<ApplicationUser> userManager)
        {
            _policyRepository = policyRepository;
            _claimRepository = claimRepository;
            _userManager = userManager;
        }

        [KernelFunction("get_all_customers")]
        [Description("Fetches up to 50 registered customers. Returns JSON array.")]
        public async Task<string> GetAllCustomersAsync()
        {
            try {
                var users = await _userManager.GetUsersInRoleAsync("Customer");
                var result = users.Take(50).Select(u => new {
                    id = u.Id,
                    fullName = u.FullName ?? "N/A",
                    email = u.Email ?? "N/A",
                    phoneNumber = u.PhoneNumber ?? "N/A",
                    isKycVerified = u.IsKycVerified,
                    createdAt = u.CreatedAt
                });
                return JsonSerializer.Serialize(result);
            } catch (Exception ex) { return $"Error: {ex.Message}"; }
        }

        [KernelFunction("get_customers_by_agent")]
        [Description("Fetches customers assigned to an agent. Requires agentId.")]
        public async Task<string> GetCustomersByAgentAsync(
            [Description("The GUID of the agent")] string agentId)
        {
            try {
                var policies = await _policyRepository.GetAgentApplicationsAsync(agentId);
                var result = policies.Take(50).Select(p => new {
                    customerId = p.UserId,
                    customerEmail = p.User?.Email ?? "N/A",
                    policyId = p.Id,
                    status = p.Status
                });
                return JsonSerializer.Serialize(result);
            } catch (Exception ex) { return $"Error: {ex.Message}"; }
        }

        [KernelFunction("get_all_policies")]
        [Description("Fetches up to 50 policies. Returns JSON array.")]
        public async Task<string> GetAllPoliciesAsync()
        {
            try {
                var policies = await _policyRepository.GetAllApplicationsAsync();
                var result = policies.Take(50).Select(p => new {
                    policyId = p.Id,
                    customerEmail = p.User?.Email ?? "N/A",
                    policyCategory = p.PolicyCategory,
                    status = p.Status,
                    totalCoverageAmount = p.TotalCoverageAmount,
                    submissionDate = p.SubmissionDate
                });
                return JsonSerializer.Serialize(result);
            } catch (Exception ex) { return $"Error: {ex.Message}"; }
        }

        [KernelFunction("get_all_claims")]
        [Description("Fetches up to 50 claims. Returns JSON array.")]
        public async Task<string> GetAllClaimsAsync()
        {
            try {
                var claims = await _claimRepository.GetAllClaimsAsync();
                var result = claims.Take(50).Select(c => new {
                    claimId = c.Id,
                    customerEmail = c.User?.Email ?? "N/A",
                    incidentType = c.IncidentType,
                    status = c.Status,
                    requestedAmount = c.RequestedAmount
                });
                return JsonSerializer.Serialize(result);
            } catch (Exception ex) { return $"Error: {ex.Message}"; }
        }

        [KernelFunction("get_all_agents")]
        [Description("Fetches all agents. Returns JSON array.")]
        public async Task<string> GetAllAgentsAsync()
        {
            try {
                var agents = await _userManager.GetUsersInRoleAsync("Agent");
                var result = agents.Select(a => new {
                    id = a.Id,
                    fullName = a.FullName ?? "N/A",
                    email = a.Email ?? "N/A"
                });
                return JsonSerializer.Serialize(result);
            } catch (Exception ex) { return $"Error: {ex.Message}"; }
        }

        [KernelFunction("get_claims_by_status")]
        [Description("Fetches up to 50 claims by status. Valid: Pending, Assigned, Approved, Rejected.")]
        public async Task<string> GetClaimsByStatusAsync(
            [Description("The claim status to filter by")] string status)
        {
            try {
                var allClaims = await _claimRepository.GetAllClaimsAsync();
                var filtered = allClaims.Where(c => c.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).Take(50);
                var result = filtered.Select(c => new {
                    claimId = c.Id,
                    customerEmail = c.User?.Email ?? "N/A",
                    incidentType = c.IncidentType,
                    status = c.Status,
                    requestedAmount = c.RequestedAmount
                });
                return JsonSerializer.Serialize(result);
            } catch (Exception ex) { return $"Error: {ex.Message}"; }
        }

        [KernelFunction("get_policies_by_status")]
        [Description("Fetches up to 50 policies by status. Valid: Active, Pending, Rejected, Expired.")]
        public async Task<string> GetPoliciesByStatusAsync(
            [Description("The policy status to filter by")] string status)
        {
            try {
                var all = await _policyRepository.GetAllApplicationsAsync();
                var filtered = all.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).Take(50);
                var result = filtered.Select(p => new {
                    policyId = p.Id,
                    customerEmail = p.User?.Email ?? "N/A",
                    policyCategory = p.PolicyCategory,
                    status = p.Status
                });
                return JsonSerializer.Serialize(result);
            } catch (Exception ex) { return $"Error: {ex.Message}"; }
        }

        [KernelFunction("count_customers_by_agent")]
        [Description("Returns the count of customers (policy holders) assigned to a specific agent. Requires agentId.")]
        public async Task<string> CountCustomersByAgentAsync(
            [Description("The GUID of the agent")] string agentId)
        {
            var count = await _policyRepository.GetCountByAgentAsync(agentId);
            return $"Agent {agentId} has {count} policy holders assigned.";
        }
    }
}
