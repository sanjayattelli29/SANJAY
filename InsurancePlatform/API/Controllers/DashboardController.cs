using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        /// <summary>
        /// Simple protected endpoint for Customers.
        /// </summary>
        [HttpGet("customer")]
        [Authorize(Roles = UserRoles.Customer)]
        public IActionResult GetCustomerDashboard()
        {
            return Ok(new { Message = "Welcome to the Customer Dashboard! Access granted." });
        }

        /// <summary>
        /// Simple protected endpoint for Agents.
        /// </summary>
        [HttpGet("agent")]
        [Authorize(Roles = UserRoles.Agent)]
        public IActionResult GetAgentDashboard()
        {
            return Ok(new { Message = "Welcome to the Agent Dashboard! Access granted." });
        }

        /// <summary>
        /// Simple protected endpoint for Claim Officers.
        /// </summary>
        [HttpGet("claim-officer")]
        [Authorize(Roles = UserRoles.ClaimOfficer)]
        public IActionResult GetClaimOfficerDashboard()
        {
            return Ok(new { Message = "Welcome to the Claim Officer Dashboard! Access granted." });
        }

        /// <summary>
        /// Simple protected endpoint for Admins.
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = UserRoles.Admin)]
        public IActionResult GetAdminDashboard()
        {
            return Ok(new { Message = "Welcome to the Admin Dashboard! Access granted." });
        }
    }
}
