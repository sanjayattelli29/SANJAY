using InsurancePlatform.DTOs.Lead;
using InsurancePlatform.DTOs.Policy;
using InsurancePlatform.Helpers;
using InsurancePlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InsurancePlatform.Controllers
{
    [Authorize(Roles = "Agent")]
    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly IPolicyService _policyService;
        private readonly ILeadService _leadService;

        public AgentController(IPolicyService policyService, ILeadService leadService)
        {
            _policyService = policyService;
            _leadService = leadService;
        }

        [HttpGet("assigned-policies")]
        public async Task<IActionResult> GetAssignedPolicies()
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var policies = await _policyService.GetPoliciesByAgentAsync(agentId);
            return Ok(ResponseWrapper<IEnumerable<PolicyResponseDto>>.SuccessResponse(policies, "Assigned policies retrieved"));
        }

        [HttpPost("approve-policy/{id}")]
        public async Task<IActionResult> ApprovePolicy(Guid id)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _policyService.ApprovePolicyAsync(id, agentId);

            if (!result) return BadRequest(ResponseWrapper<bool>.ErrorResponse("Policy approval failed or unauthorized"));

            return Ok(ResponseWrapper<bool>.SuccessResponse(true, "Policy approved successfully"));
        }

        [HttpPost("reject-policy/{id}")]
        public async Task<IActionResult> RejectPolicy(Guid id, [FromBody] string reason)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _policyService.RejectPolicyAsync(id, agentId, reason);

            if (!result) return BadRequest(ResponseWrapper<bool>.ErrorResponse("Policy rejection failed or unauthorized"));

            return Ok(ResponseWrapper<bool>.SuccessResponse(true, "Policy rejected"));
        }

        // --- Lead Management ---

        [HttpGet("assigned-leads")]
        public async Task<IActionResult> GetAssignedLeads()
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var leads = await _leadService.GetLeadsByAgentAsync(agentId);
            return Ok(ResponseWrapper<IEnumerable<LeadResponseDto>>.SuccessResponse(leads, "Assigned leads retrieved"));
        }

        [HttpPost("update-lead-status/{leadId}")]
        public async Task<IActionResult> UpdateLeadStatus(Guid leadId, [FromBody] string status)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _leadService.UpdateLeadStatusAsync(leadId, agentId, status);
            if (!result) return BadRequest(ResponseWrapper<bool>.ErrorResponse("Status update failed"));
            return Ok(ResponseWrapper<bool>.SuccessResponse(true, "Lead status updated"));
        }

        [HttpPost("convert-lead/{leadId}")]
        public async Task<IActionResult> ConvertLead(Guid leadId)
        {
            var agentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _leadService.ConvertLeadToPolicyAsync(leadId, agentId);
            if (!result) return BadRequest(ResponseWrapper<bool>.ErrorResponse("Lead conversion failed"));
            return Ok(ResponseWrapper<bool>.SuccessResponse(true, "Lead converted to policy draft"));
        }
    }
}
