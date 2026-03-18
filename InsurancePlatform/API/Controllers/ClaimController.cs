using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    // this handles everything related to insurance claims
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClaimController : ControllerBase
    {
        private readonly IClaimProcessor _claimProcessor;

        public ClaimController(IClaimProcessor claimProcessor)
        {
            _claimProcessor = claimProcessor;
        }

        // --- Customer Endpoints ---

        // Endpoint for customers to raise a new insurance claim
        // Accepts claim details and uploaded documents using FromForm
        [HttpPost("raise")]
        [Authorize(Roles = UserRoles.Customer)]
        public async Task<IActionResult> RaiseClaim([FromForm] RaiseClaimRequest request)
        {
            // Extract logged-in user ID from JWT token claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            try
            {
                // Call application service to process claim creation
                var result = await _claimProcessor.RaiseClaimAsync(userId, request);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                // Returns error response if claim creation fails
                return BadRequest(new { Message = ex.Message });
            }
        }



        // customer sees all claims they have made
        [HttpGet("my-claims")]
        [Authorize(Roles = UserRoles.Customer)]
        public async Task<IActionResult> GetMyClaims()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var claims = await _claimProcessor.GetCustomerClaimsAsync(userId);
            return Ok(claims);
        }

        // --- Admin Endpoints ---

        // admin sees claims that nobody is working on yet
        [HttpGet("admin/pending")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetPendingClaims()
        {
            var claims = await _claimProcessor.GetPendingClaimsAsync();
            return Ok(claims);
        }

        // admin gets list of claim officers and their current workload (number of claims assigned to them)
        [HttpGet("admin/officers")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetClaimOfficers()
        {
            var officers = await _claimProcessor.GetClaimOfficersWithWorkloadAsync();
            return Ok(officers);
        }

        // admin gives a claim to an officer to check
        [HttpPost("admin/assign")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> AssignOfficer([FromBody] AssignOfficerRequest request)
        {
            var result = await _claimProcessor.AssignClaimOfficerAsync(request.ClaimId, request.OfficerId);
            if (!result) return BadRequest(new { Message = "Assignment failed." });
            return Ok(new { Message = "Officer assigned successfully." });
        }

        // --- Claim Officer Endpoints ---

        // officer sees the claims they need to check
        [HttpGet("officer/my-requests")]
        [Authorize(Roles = UserRoles.ClaimOfficer)]
        public async Task<IActionResult> GetOfficerRequests()
        {
            var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (officerId == null) return Unauthorized();

            var requests = await _claimProcessor.GetOfficerClaimsAsync(officerId);
            return Ok(requests);
        }

        // officer says yes or no to the claim
        [HttpPost("officer/review")]
        [Authorize(Roles = UserRoles.ClaimOfficer)]
        public async Task<IActionResult> ReviewClaim([FromBody] ReviewClaimRequest request)
        {
            var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (officerId == null) return Unauthorized();

            var result = await _claimProcessor.ReviewClaimAsync(request.ClaimId, request.Status, officerId, request.Remarks, request.ApprovedAmount);
            if (!result) return BadRequest(new { Message = "Review failed." });
            return Ok(new { Message = $"Claim {request.Status} successfully." });
        }

        // --- Agent Endpoints ---

        // agent sees claims from their own customers
        [HttpGet("agent/customer-claims")]
        [Authorize(Roles = UserRoles.Agent)]
        public async Task<IActionResult> GetAgentCustomerClaims()
        {
            var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (agentId == null) return Unauthorized();

            var claims = await _claimProcessor.GetAgentClaimsAsync(agentId);
            return Ok(claims);
        }

        // find claim details for a specific policy
        [HttpGet("policy/{policyId}")]
        public async Task<IActionResult> GetClaimByPolicyId(string policyId)
        {
            var claim = await _claimProcessor.GetClaimByPolicyIdAsync(policyId);
            return Ok(claim);
        }
    }
}
