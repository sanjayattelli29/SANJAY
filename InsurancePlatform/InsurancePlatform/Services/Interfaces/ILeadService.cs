using InsurancePlatform.DTOs.Lead;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsurancePlatform.Services.Interfaces
{
    public interface ILeadService
    {
        Task<LeadResponseDto> CreateLeadAsync(Guid customerId);
        Task<LeadResponseDto> ProcessChatMessageAsync(Guid leadId, string message);
        Task<IEnumerable<LeadResponseDto>> GetLeadsByAgentAsync(Guid agentId);
        Task<bool> UpdateLeadStatusAsync(Guid leadId, Guid agentId, string status);
        Task<bool> ConvertLeadToPolicyAsync(Guid leadId, Guid agentId);
    }
}
