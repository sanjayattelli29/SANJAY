using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // these are just simple test routes to check if roles are working
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        // customer's home page check
        [HttpGet("customer")]
        [Authorize(Roles = UserRoles.Customer)]
        public IActionResult GetCustomerDashboard()
        {
            return Ok(new { Message = "Welcome to the Customer Dashboard! Access granted." });
        }

        // agent's home page check
        [HttpGet("agent")]
        [Authorize(Roles = UserRoles.Agent)]
        public IActionResult GetAgentDashboard()
        {
            return Ok(new { Message = "Welcome to the Agent Dashboard! Access granted." });
        }

        // claim officer's home page check
        [HttpGet("claim-officer")]
        [Authorize(Roles = UserRoles.ClaimOfficer)]
        public IActionResult GetClaimOfficerDashboard()
        {
            return Ok(new { Message = "Welcome to the Claim Officer Dashboard! Access granted." });
        }

        // admin's home page check
        [HttpGet("admin")]
        [Authorize(Roles = UserRoles.Admin)]
        public IActionResult GetAdminDashboard()
        {
            return Ok(new { Message = "Welcome to the Admin Dashboard! Access granted." });
        }
    }
}
