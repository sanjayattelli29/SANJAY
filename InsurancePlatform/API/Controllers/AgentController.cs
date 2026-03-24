using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    // this handles what an agent sees and does
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Agent)]
    public class AgentController : ControllerBase
    {
        private readonly IPolicyManager _policyManager;

        public AgentController(IPolicyManager policyManager)
        {
            _policyManager = policyManager;
        }

        // get the insurance applications assigned to me
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var requests = await _policyManager.GetAgentApplicationsAsync(agentId);
            return Ok(requests);
        }

        // approve or reject a customer's application
        [HttpPost("review-request")]
        public async Task<IActionResult> ReviewRequest([FromBody] ReviewApplicationRequest request)
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var success = await _policyManager.ReviewApplicationAsync(request.ApplicationId, request.Status, agentId);
            if (!success) return BadRequest(new { Message = "Review failed" });
            
            return Ok(new { Message = $"Application {request.Status} successfully" });
        }

        // list of customers who bought policies through me
        [HttpGet("my-customers")]
        public async Task<IActionResult> GetMyCustomers()
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var customers = await _policyManager.GetAgentCustomersAsync(agentId);
            return Ok(customers);
        }

        // see how much money i made from commissions
        [HttpGet("commission-stats")]
        public async Task<IActionResult> GetCommissionStats()
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var stats = await _policyManager.GetAgentCommissionStatsAsync(agentId);
            return Ok(stats);
        }

        // see charts and data about my performance
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var analytics = await _policyManager.GetAgentAnalyticsAsync(agentId);
            return Ok(analytics);
        }
    }

    public class ReviewApplicationRequest
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Approved or Rejected
    }
}
