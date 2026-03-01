using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; // api base
using System.Security.Claims; // identity data

namespace API.Controllers
{
    // this handles general stuff about insurance policies
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // login check
    public class PolicyController : ControllerBase // policy web api
    {
        private readonly IPolicyService _policyService;

        public PolicyController(IPolicyService policyService)
        {
            _policyService = policyService; // set policy logic
        }

        // see the plans and category details
        [HttpGet("configuration")]
        [AllowAnonymous] // anyone can see
        public async Task<IActionResult> GetConfiguration() // fetch settings
        {
            var config = await _policyService.GetConfigurationAsync();
            return Ok(config);
        }

        // calculate how much premium user has to pay
        [HttpPost("calculate-premium")] // post request for price
        public async Task<IActionResult> CalculatePremium([FromBody] PolicyApplicationRequest request) // receives data
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

        // customer applies for a new insurance plan
        [HttpPost("apply")] // post request for signup
        public async Task<IActionResult> Apply([FromBody] PolicyApplicationRequest request) // receives form data
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

        // customer sees all policies they have bought
        [HttpGet("my-policies")] // get request for my list
        public async Task<IActionResult> GetMyPolicies() // fetch my records
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var policies = await _policyService.GetUserPoliciesAsync(userId);
            return Ok(policies);
        }
    }
}
// policy controller end
