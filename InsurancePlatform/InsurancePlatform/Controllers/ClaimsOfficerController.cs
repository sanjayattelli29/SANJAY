using InsurancePlatform.DTOs.Claim;
using InsurancePlatform.Helpers;
using InsurancePlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InsurancePlatform.Controllers
{
    [Authorize(Roles = "ClaimsOfficer")]
    [ApiController]
    [Route("api/[controller]")]
    public class ClaimsOfficerController : ControllerBase
    {
        private readonly IClaimService _claimService;

        public ClaimsOfficerController(IClaimService claimService)
        {
            _claimService = claimService;
        }

        [HttpGet("assigned-claims")]
        public async Task<IActionResult> GetAssignedClaims()
        {
            var officerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var claims = await _claimService.GetClaimsByOfficerAsync(officerId);
            return Ok(ResponseWrapper<IEnumerable<ClaimResponseDto>>.SuccessResponse(claims, "Assigned claims retrieved"));
        }

        [HttpPost("approve/{claimId}")]
        public async Task<IActionResult> ApproveClaim(Guid claimId, [FromBody] decimal approvedAmount)
        {
            var officerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _claimService.ApproveClaimAsync(claimId, officerId, approvedAmount);
            if (!result) return BadRequest(ResponseWrapper<bool>.ErrorResponse("Approval failed"));
            return Ok(ResponseWrapper<bool>.SuccessResponse(true, "Claim approved"));
        }

        [HttpPost("reject/{claimId}")]
        public async Task<IActionResult> RejectClaim(Guid claimId, [FromBody] string reason)
        {
            var officerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _claimService.RejectClaimAsync(claimId, officerId, reason);
            if (!result) return BadRequest(ResponseWrapper<bool>.ErrorResponse("Rejection failed"));
            return Ok(ResponseWrapper<bool>.SuccessResponse(true, "Claim rejected"));
        }

        [HttpPost("settle/{claimId}")]
        public async Task<IActionResult> SettleClaim(Guid claimId)
        {
            var officerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _claimService.SettleClaimAsync(claimId, officerId);
            if (!result) return BadRequest(ResponseWrapper<bool>.ErrorResponse("Settlement failed"));
            return Ok(ResponseWrapper<bool>.SuccessResponse(true, "Claim settled"));
        }
    }
}
