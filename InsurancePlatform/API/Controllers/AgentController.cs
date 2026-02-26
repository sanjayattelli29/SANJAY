using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Agent)]
    public class AgentController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public AgentController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var requests = await _policyService.GetAgentApplicationsAsync(agentId);
            return Ok(requests);
        }

        [HttpPost("review-request")]
        public async Task<IActionResult> ReviewRequest([FromBody] ReviewApplicationRequest request)
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var success = await _policyService.ReviewApplicationAsync(request.ApplicationId, request.Status, agentId);
            if (!success) return BadRequest(new { Message = "Review failed" });
            
            return Ok(new { Message = $"Application {request.Status} successfully" });
        }

        [HttpGet("commission-stats")]
        public async Task<IActionResult> GetCommissionStats()
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var stats = await _policyService.GetAgentCommissionStatsAsync(agentId);
            return Ok(stats);
        }
    }

    public class ReviewApplicationRequest
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Approved or Rejected
    }
}
