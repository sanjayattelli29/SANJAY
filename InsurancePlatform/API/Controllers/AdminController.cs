using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization; // for security roles
using Microsoft.AspNetCore.Mvc; // for web api tools

namespace API.Controllers
{
    // this controller is only for the big boss (admin)
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Admin)] // only admin allowed
    public class AdminController : ControllerBase // admin web api
    {
        private readonly IAuthService _authService;
        private readonly IPolicyService _policyService;
        private readonly IClaimService _claimService;

        public AdminController(IAuthService authService, IPolicyService policyService, IClaimService claimService)
        {
            _authService = authService;
            _policyService = policyService;
            _claimService = claimService; // set claim service
        }

        // adds a new agent to the system
        [HttpPost("create-agent")] // post request to add agent
        public async Task<IActionResult> CreateAgent([FromBody] CreateAgentDto agentDto) // receives agent json
        {
            var result = await _authService.CreateAgentAsync(agentDto);
            return Ok(result);
        }

        // adds a new claim officer to the system
        [HttpPost("create-claim-officer")] // post request to add officer
        public async Task<IActionResult> CreateClaimOfficer([FromBody] CreateClaimOfficerDto claimOfficerDto) // receives officer json
        {
            var result = await _authService.CreateClaimOfficerAsync(claimOfficerDto);
            return Ok(result);
        }

        // get list of all agents we have
        [HttpGet("agents")]
        public async Task<IActionResult> GetAgents()
        {
            var result = await _authService.GetUsersByRoleAsync(UserRoles.Agent);
            return Ok(result);
        }

        // get list of all claim officers
        [HttpGet("claim-officers")]
        public async Task<IActionResult> GetClaimOfficers()
        {
            var result = await _authService.GetUsersByRoleAsync(UserRoles.ClaimOfficer);
            return Ok(result);
        }

        // remove a user from database using their id
        [HttpDelete("delete-user/{id}")] // delete request for user
        public async Task<IActionResult> DeleteUser(string id) // receives user id string
        {
            var result = await _authService.DeleteUserAsync(id);
            return Ok(result);
        }

        // see all policy applications from everyone
        [HttpGet("policy-requests")]
        public async Task<IActionResult> GetAllPolicyRequests()
        {
            var requests = await _policyService.GetAllApplicationsAsync();
            return Ok(requests);
        }

        // see which agent has how much work
        [HttpGet("agents-with-load")]
        public async Task<IActionResult> GetAgentsWithLoad()
        {
            var agents = await _policyService.GetAgentsWithWorkloadAsync();
            return Ok(agents);
        }

        // give a policy application to a specific agent
        [HttpPost("assign-agent")] // post request for assignment
        public async Task<IActionResult> AssignAgent([FromBody] AssignAgentRequest request) // receives assignment data
        {
            var success = await _policyService.AssignAgentAsync(request.ApplicationId, request.AgentId);
            if (!success) return BadRequest(new { Message = "Assignment failed" });
            return Ok(new { Message = "Agent assigned successfully" });
        }

        // get every single user in the system
        [HttpGet("all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(users);
        }

        // get every single insurance claim ever made
        [HttpGet("all-claims")]
        public async Task<IActionResult> GetAllClaimsMaster()
        {
            var claims = await _claimService.GetAllClaimsAsync();
            return Ok(claims);
        }

        // get some numbers for the admin dashboard
        [HttpGet("admin-stats")]
        public async Task<IActionResult> GetAdminStats()
        {
            var stats = await _claimService.GetAdminStatsAsync();
            return Ok(stats);
        }
    }
// admin controller ends here
    public class AssignAgentRequest
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
    }
}
