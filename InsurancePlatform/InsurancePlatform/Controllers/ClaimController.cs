using InsurancePlatform.DTOs.Claim;
using InsurancePlatform.Helpers;
using InsurancePlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InsurancePlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClaimController : ControllerBase
    {
        private readonly IClaimService _claimService;

        public ClaimController(IClaimService claimService)
        {
            _claimService = claimService;
        }

        [HttpPost("submit")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SubmitClaim(CreateClaimDto dto)
        {
            var customerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _claimService.CreateClaimAsync(customerId, dto);
            return Ok(ResponseWrapper<ClaimResponseDto>.SuccessResponse(result, "Claim submitted and assigned to an officer."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClaim(Guid id)
        {
            var result = await _claimService.GetClaimByIdAsync(id);
            if (result == null) return NotFound(ResponseWrapper<string>.ErrorResponse("Claim not found"));
            return Ok(ResponseWrapper<ClaimResponseDto>.SuccessResponse(result));
        }
    }
}
