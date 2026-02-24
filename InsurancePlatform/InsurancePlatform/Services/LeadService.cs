using InsurancePlatform.Data;
using InsurancePlatform.DTOs.Lead;
using InsurancePlatform.Models;
using InsurancePlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsurancePlatform.Services
{
    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAiService _aiService;
        private readonly IAssignmentService _assignmentService;

        public LeadService(ApplicationDbContext context, IAiService aiService, IAssignmentService assignmentService)
        {
            _context = context;
            _aiService = aiService;
            _assignmentService = assignmentService;
        }

        public async Task<LeadResponseDto> CreateLeadAsync(Guid customerId)
        {
            var lead = new Lead
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                LeadScore = 0,
                LeadStatus = LeadStatus.New,
                Source = LeadSource.AIChatbot,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Leads.AddAsync(lead);
            await _context.SaveChangesAsync();

            return await MapToResponse(lead);
        }

        public async Task<LeadResponseDto> ProcessChatMessageAsync(Guid leadId, string message)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null) throw new Exception("Lead not found");

            var intentScore = await _aiService.AnalyzeBuyingIntentAsync(message);
            lead.LeadScore += intentScore;

            // Trigger assignment threshold
            if (lead.LeadScore >= 30 && (lead.AssignedAgentId == Guid.Empty || lead.AssignedAgentId == default))
            {
                var agentId = await _assignmentService.GetLeastLoadedAgentByLeadCountAsync();
                lead.AssignedAgentId = agentId;
                
                var agentProfile = await _context.AgentProfiles.FirstAsync(a => a.UserId == agentId);
                agentProfile.ActiveLeadCount++;
            }

            await _context.SaveChangesAsync();
            return await MapToResponse(lead);
        }

        public async Task<IEnumerable<LeadResponseDto>> GetLeadsByAgentAsync(Guid agentId)
        {
            var leads = await _context.Leads
                .Include(l => l.Customer)
                .ThenInclude(c => c.User)
                .Where(l => l.AssignedAgentId == agentId)
                .ToListAsync();

            var response = new List<LeadResponseDto>();
            foreach (var l in leads) response.Add(await MapToResponse(l));
            return response;
        }

        public async Task<bool> UpdateLeadStatusAsync(Guid leadId, Guid agentId, string status)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null || lead.AssignedAgentId != agentId) return false;

            if (Enum.TryParse<LeadStatus>(status, true, out var newStatus))
            {
                lead.LeadStatus = newStatus;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> ConvertLeadToPolicyAsync(Guid leadId, Guid agentId)
        {
            var lead = await _context.Leads
                .Include(l => l.Customer)
                .FirstOrDefaultAsync(l => l.Id == leadId);

            if (lead == null || lead.AssignedAgentId != agentId) return false;
            if (lead.LeadStatus == LeadStatus.Converted) return false;

            lead.LeadStatus = LeadStatus.Converted;

            // Create Policy Draft
            var policy = new Policy
            {
                Id = Guid.NewGuid(),
                PolicyNumber = "POL-L-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                CustomerId = lead.CustomerId,
                AssignedAgentId = agentId,
                Status = PolicyStatus.ActiveOrderDraft,
                CreatedAt = DateTime.UtcNow,
                PolicyStartDate = DateTime.UtcNow,
                PolicyEndDate = DateTime.UtcNow.AddYears(1)
            };

            var agentProfile = await _context.AgentProfiles.FirstAsync(a => a.UserId == agentId);
            agentProfile.ActiveLeadCount--;
            agentProfile.ActiveCustomerCount++;

            await _context.Policies.AddAsync(policy);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<LeadResponseDto> MapToResponse(Lead l)
        {
            var customerUser = await _context.Users.FindAsync(l.CustomerId);
            var agentUser = (l.AssignedAgentId != Guid.Empty && l.AssignedAgentId != default) ? await _context.Users.FindAsync(l.AssignedAgentId) : null;

            return new LeadResponseDto
            {
                LeadId = l.Id,
                CustomerName = customerUser != null ? $"{customerUser.FirstName} {customerUser.LastName}" : "Unknown",
                LeadScore = l.LeadScore,
                LeadStatus = l.LeadStatus.ToString(),
                Source = l.Source.ToString(),
                AssignedAgentName = agentUser != null ? $"{agentUser.FirstName} {agentUser.LastName}" : "Pending AI Escalation",
                CreatedAt = l.CreatedAt
            };
        }
    }
}
