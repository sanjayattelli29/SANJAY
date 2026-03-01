using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; // api tools
using System.Security.Claims; // identity claims
using System.Threading.Tasks; // async support

namespace API.Controllers
{
    // this handles everything related to insurance claims
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // login required
    public class ClaimController : ControllerBase // claim web api
    {
        private readonly IClaimService _claimService;

        public ClaimController(IClaimService claimService)
        {
            _claimService = claimService; // set claim logic
        }

        // --- Customer Endpoints ---

        // customer uses this to ask for money after an accident
        [HttpPost("raise")] // post request for new claim
        [Authorize(Roles = UserRoles.Customer)] // only customers can ask
        public async Task<IActionResult> RaiseClaim([FromForm] RaiseClaimRequest request) // receives form data
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            try
            {
                var result = await _claimService.RaiseClaimAsync(userId, request);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // customer sees all claims they have made
        [HttpGet("my-claims")] // get request for my history
        [Authorize(Roles = UserRoles.Customer)] // for customer role
        public async Task<IActionResult> GetMyClaims() // fetch my claims
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var claims = await _claimService.GetCustomerClaimsAsync(userId);
            return Ok(claims);
        }

        // --- Admin Endpoints ---

        // admin sees claims that nobody is working on yet
        [HttpGet("admin/pending")] // see new claims
        [Authorize(Roles = UserRoles.Admin)] // boss view
        public async Task<IActionResult> GetPendingClaims() // fetch pending
        {
            var claims = await _claimService.GetPendingClaimsAsync();
            return Ok(claims);
        }

        // admin gets list of people who check claims
        [HttpGet("admin/officers")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetClaimOfficers()
        {
            var officers = await _claimService.GetClaimOfficersWithWorkloadAsync();
            return Ok(officers);
        }

        // admin gives a claim to an officer to check
        [HttpPost("admin/assign")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> AssignOfficer([FromBody] AssignOfficerRequest request)
        {
            var result = await _claimService.AssignClaimOfficerAsync(request.ClaimId, request.OfficerId);
            if (!result) return BadRequest(new { Message = "Assignment failed." });
            return Ok(new { Message = "Officer assigned successfully." });
        }

        // --- Claim Officer Endpoints ---

        // officer sees the claims they need to check
        [HttpGet("officer/my-requests")] // seen by officer
        [Authorize(Roles = UserRoles.ClaimOfficer)] // officer role check
        public async Task<IActionResult> GetOfficerRequests() // fetch assignments
        {
            var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (officerId == null) return Unauthorized();

            var requests = await _claimService.GetOfficerClaimsAsync(officerId);
            return Ok(requests);
        }

        // officer says yes or no to the claim
        [HttpPost("officer/review")]
        [Authorize(Roles = UserRoles.ClaimOfficer)]
        public async Task<IActionResult> ReviewClaim([FromBody] ReviewClaimRequest request)
        {
            var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (officerId == null) return Unauthorized();

            var result = await _claimService.ReviewClaimAsync(request.ClaimId, request.Status, officerId, request.Remarks, request.ApprovedAmount);
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

            var claims = await _claimService.GetAgentClaimsAsync(agentId);
            return Ok(claims);
        }

        // find claim details for a specific policy
        [HttpGet("policy/{policyId}")]
        public async Task<IActionResult> GetClaimByPolicyId(string policyId)
        {
            var claim = await _claimService.GetClaimByPolicyIdAsync(policyId);
            return Ok(claim);
        }
    }
// claim controller logic ends
    public class AssignOfficerRequest
    {
        public string ClaimId { get; set; } = string.Empty;
        public string OfficerId { get; set; } = string.Empty;
    }

    public class ReviewClaimRequest
    {
        public string ClaimId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Approved or Rejected
        public string Remarks { get; set; } = string.Empty;
        public decimal ApprovedAmount { get; set; }

    }
}
