using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    // this handles general stuff about insurance policies
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

        // see the plans and category details
        [HttpGet("configuration")]
        [AllowAnonymous]
        public async Task<IActionResult> GetConfiguration()
        {
            var config = await _policyService.GetConfigurationAsync();
            return Ok(config);
        }

        // calculate how much premium user has to pay
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

        // customer applies for a new insurance plan
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

        // customer uploads documents for their application
        [HttpPost("submit-documents")]
        public async Task<IActionResult> SubmitDocuments([FromForm] SubmitPolicyDocumentsRequest request)
        {
            try
            {
                var result = await _policyService.SubmitPolicyDocumentsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    
        [HttpPost("upload-document")]
        public async Task<IActionResult> UploadDocument(IFormFile file, [FromForm] string folder = "general")
        {
            if (file == null || file.Length == 0) return BadRequest(new { Message = "No file uploaded" });
            
            try
            {
                using var stream = file.OpenReadStream();
                var url = await _policyService.UploadGeneralFileAsync(stream, file.FileName, folder);
                return Ok(new { Url = url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
