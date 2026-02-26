using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
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

        [HttpGet("configuration")]
        [AllowAnonymous]
        public async Task<IActionResult> GetConfiguration()
        {
            var config = await _policyService.GetConfigurationAsync();
            return Ok(config);
        }

        [HttpPost("calculate-premium")]
        public async Task<IActionResult> CalculatePremium([FromBody] PolicyApplicationRequest request)
        {
            try
            {
                var premium = await _policyService.CalculatePremiumAsync(request);
                return Ok(new { Premium = premium });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] PolicyApplicationRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var result = await _policyService.ApplyForPolicyAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("my-policies")]
        public async Task<IActionResult> GetMyPolicies()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var policies = await _policyService.GetUserPoliciesAsync(userId);
            return Ok(policies);
        }
    }
}
