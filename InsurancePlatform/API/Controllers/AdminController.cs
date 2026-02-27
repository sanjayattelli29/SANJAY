using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IPolicyService _policyService;
        private readonly IClaimService _claimService;

        public AdminController(IAuthService authService, IPolicyService policyService, IClaimService claimService)
        {
            _authService = authService;
            _policyService = policyService;
            _claimService = claimService;
        }

        /// <summary>
        /// Creates a new Agent. Restricted to Admin role.
        /// </summary>
        [HttpPost("create-agent")]
        public async Task<IActionResult> CreateAgent([FromBody] CreateAgentDto agentDto)
        {
            var result = await _authService.CreateAgentAsync(agentDto);
            return Ok(result);
        }

        /// <summary>
        /// Creates a new Claim Officer. Restricted to Admin role.
        /// </summary>
        [HttpPost("create-claim-officer")]
        public async Task<IActionResult> CreateClaimOfficer([FromBody] CreateClaimOfficerDto claimOfficerDto)
        {
            var result = await _authService.CreateClaimOfficerAsync(claimOfficerDto);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves all Agents.
        /// </summary>
        [HttpGet("agents")]
        public async Task<IActionResult> GetAgents()
        {
            var result = await _authService.GetUsersByRoleAsync(UserRoles.Agent);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves all Claim Officers.
        /// </summary>
        [HttpGet("claim-officers")]
        public async Task<IActionResult> GetClaimOfficers()
        {
            var result = await _authService.GetUsersByRoleAsync(UserRoles.ClaimOfficer);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a user by ID.
        /// </summary>
        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var result = await _authService.DeleteUserAsync(id);
            return Ok(result);
        }

        [HttpGet("policy-requests")]
        public async Task<IActionResult> GetAllPolicyRequests()
        {
            var requests = await _policyService.GetAllApplicationsAsync();
            return Ok(requests);
        }

        [HttpGet("agents-with-load")]
        public async Task<IActionResult> GetAgentsWithLoad()
        {
            var agents = await _policyService.GetAgentsWithWorkloadAsync();
            return Ok(agents);
        }

        [HttpPost("assign-agent")]
        public async Task<IActionResult> AssignAgent([FromBody] AssignAgentRequest request)
        {
            var success = await _policyService.AssignAgentAsync(request.ApplicationId, request.AgentId);
            if (!success) return BadRequest(new { Message = "Assignment failed" });
            return Ok(new { Message = "Agent assigned successfully" });
        }

        [HttpGet("all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("all-claims")]
        public async Task<IActionResult> GetAllClaimsMaster()
        {
            var claims = await _claimService.GetAllClaimsAsync();
            return Ok(claims);
        }

        [HttpGet("admin-stats")]
        public async Task<IActionResult> GetAdminStats()
        {
            var stats = await _claimService.GetAdminStatsAsync();
            return Ok(stats);
        }
    }

    public class AssignAgentRequest
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
    }
}
