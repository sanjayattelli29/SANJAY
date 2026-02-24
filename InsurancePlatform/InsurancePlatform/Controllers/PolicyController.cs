using InsurancePlatform.DTOs.Policy;
using InsurancePlatform.Helpers;
using InsurancePlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsurancePlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PolicyController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public PolicyController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> CreatePolicy(CreatePolicyDto dto)
        {
            var result = await _policyService.CreatePolicyAsync(dto);
            return Ok(ResponseWrapper<PolicyResponseDto>.SuccessResponse(result, "Policy created successfully"));
        }

        [HttpPost("buy")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> BuyPolicy(CreatePolicyDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var result = await _policyService.DirectBuyPolicyAsync(userId, dto);
            return Ok(ResponseWrapper<PolicyResponseDto>.SuccessResponse(result, "Policy purchase request submitted and assigned to an agent."));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPolicies()
        {
            var result = await _policyService.GetAllPoliciesAsync();
            return Ok(ResponseWrapper<IEnumerable<PolicyResponseDto>>.SuccessResponse(result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPolicyById(Guid id)
        {
            var result = await _policyService.GetPolicyByIdAsync(id);
            if (result == null) return NotFound(ResponseWrapper<string>.ErrorResponse("Policy not found"));
            return Ok(ResponseWrapper<PolicyResponseDto>.SuccessResponse(result));
        }
    }
}
