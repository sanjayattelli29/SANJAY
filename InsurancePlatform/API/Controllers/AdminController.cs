using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // this controller is only for the big boss (admin)
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly IPolicyManager _policyManager;
        private readonly IClaimProcessor _claimProcessor;

        public AdminController(IIdentityService identityService, IPolicyManager policyManager, IClaimProcessor claimProcessor)
        {
            _identityService = identityService;
            _policyManager = policyManager;
            _claimProcessor = claimProcessor;
        }

        // adds a new agent to the system
        [HttpPost("create-agent")]
        public async Task<IActionResult> CreateAgent([FromBody] CreateAgentDto agentDto)
        {
            var result = await _identityService.CreateAgentAsync(agentDto);
            return Ok(result);
        }

        // adds a new claim officer to the system
        [HttpPost("create-claim-officer")]
        public async Task<IActionResult> CreateClaimOfficer([FromBody] CreateClaimOfficerDto claimOfficerDto)
        {
            var result = await _identityService.CreateClaimOfficerAsync(claimOfficerDto);
            return Ok(result);
        }

        // get list of all agents we have
        [HttpGet("agents")]
        public async Task<IActionResult> GetAgents()
        {
            var result = await _identityService.GetUsersByRoleAsync(UserRoles.Agent);
            return Ok(result);
        }

        // get list of all claim officers
        [HttpGet("claim-officers")]
        public async Task<IActionResult> GetClaimOfficers()
        {
            var result = await _identityService.GetUsersByRoleAsync(UserRoles.ClaimOfficer);
            return Ok(result);
        }

        // remove a user from database using their id
        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var result = await _identityService.DeleteUserAsync(id);
            return Ok(result);
        }

        // see all policy applications from everyone
        [HttpGet("policy-requests")]
        public async Task<IActionResult> GetAllPolicyRequests()
        {
            var requests = await _policyManager.GetAllApplicationsAsync();
            return Ok(requests);
        }

        // see which agent has how much work
        [HttpGet("agents-with-load")]
        public async Task<IActionResult> GetAgentsWithLoad()
        {
            var agents = await _policyManager.GetAgentsWithWorkloadAsync();
            return Ok(agents);
        }

        // give a policy application to a specific agent
        [HttpPost("assign-agent")]
        public async Task<IActionResult> AssignAgent([FromBody] AssignAgentRequest request)
        {
            var success = await _policyManager.AssignAgentAsync(request.ApplicationId, request.AgentId);
            if (!success) return BadRequest(new { Message = "Assignment failed" });
            return Ok(new { Message = "Agent assigned successfully" });
        }

        // get every single user in the system
        [HttpGet("all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _identityService.GetAllUsersAsync();
            return Ok(users);
        }

        // get every single insurance claim ever made
        [HttpGet("all-claims")]
        public async Task<IActionResult> GetAllClaimsMaster()
        {
            var claims = await _claimProcessor.GetAllClaimsAsync();
            return Ok(claims);
        }

        // get some numbers for the admin dashboard
        [HttpGet("admin-stats")]
        public async Task<IActionResult> GetAdminStats()
        {
            var stats = await _claimProcessor.GetAdminStatsAsync();
            return Ok(stats);
        }
    }

    public class AssignAgentRequest
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
    }
}
