using Domain.Enums;
using Microsoft.AspNetCore.Authorization; // security
using Microsoft.AspNetCore.Mvc; // api toolkit

namespace API.Controllers
{
    // these are just simple test routes to check if roles are working
    [ApiController]
    [Route("api/[controller]")] // url path
    public class DashboardController : ControllerBase // test controller
    {
        // customer's home page check
        [HttpGet("customer")]
        [Authorize(Roles = UserRoles.Customer)] // check for customer
        public IActionResult GetCustomerDashboard() // returns ok
        {
            return Ok(new { Message = "Welcome to the Customer Dashboard! Access granted." });
        }

        // agent's home page check
        [HttpGet("agent")]
        [Authorize(Roles = UserRoles.Agent)] // check for agent
        public IActionResult GetAgentDashboard() // returns ok
        {
            return Ok(new { Message = "Welcome to the Agent Dashboard! Access granted." });
        }

        // claim officer's home page check
        [HttpGet("claim-officer")]
        [Authorize(Roles = UserRoles.ClaimOfficer)] // check for officer
        public IActionResult GetClaimOfficerDashboard() // returns ok
        {
            return Ok(new { Message = "Welcome to the Claim Officer Dashboard! Access granted." });
        }

        // admin's home page check
        [HttpGet("admin")]
        [Authorize(Roles = UserRoles.Admin)] // check for admin
        public IActionResult GetAdminDashboard() // returns ok
        {
            return Ok(new { Message = "Welcome to the Admin Dashboard! Access granted." });
        }
    }
}
// dashboard test controller ends
