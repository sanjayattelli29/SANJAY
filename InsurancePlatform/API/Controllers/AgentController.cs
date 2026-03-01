using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; // web tools
using System.Security.Claims; // identity claims

namespace API.Controllers
{
    // this handles what an agent sees and does
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Agent)] // only for agents
    public class AgentController : ControllerBase // agent server side
    {
        private readonly IPolicyService _policyService;

        public AgentController(IPolicyService policyService)
        {
            _policyService = policyService; // set policy logic
        }

        // get the insurance applications assigned to me
        [HttpGet("my-requests")] // get request for work
        public async Task<IActionResult> GetMyRequests() // fetch my tasks
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var requests = await _policyService.GetAgentApplicationsAsync(agentId);
            return Ok(requests);
        }

        // approve or reject a customer's application
        [HttpPost("review-request")] // post request to check policy
        public async Task<IActionResult> ReviewRequest([FromBody] ReviewApplicationRequest request) // receives review result
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var success = await _policyService.ReviewApplicationAsync(request.ApplicationId, request.Status, agentId);
            if (!success) return BadRequest(new { Message = "Review failed" });
            
            return Ok(new { Message = $"Application {request.Status} successfully" });
        }

        // list of customers who bought policies through me
        [HttpGet("my-customers")]
        public async Task<IActionResult> GetMyCustomers()
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var customers = await _policyService.GetAgentCustomersAsync(agentId); // fetch customers
            return Ok(customers); // return result
        }

        // see how much money i made from commissions
        [HttpGet("commission-stats")]
        public async Task<IActionResult> GetCommissionStats()
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var stats = await _policyService.GetAgentCommissionStatsAsync(agentId); // fetch money stats
            return Ok(stats); // return result
        }

        // see charts and data about my performance
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(agentId)) return Unauthorized();

            var analytics = await _policyService.GetAgentAnalyticsAsync(agentId);
            return Ok(analytics);
        }
    }
    // agent logic controller ends
    public class ReviewApplicationRequest
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Approved or Rejected
    }
}
