using InsurancePlatform.DTOs.Lead;
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
    public class LeadController : ControllerBase
    {
        private readonly ILeadService _leadService;

        public LeadController(ILeadService leadService)
        {
            _leadService = leadService;
        }

        [HttpPost("interested")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ExpressInterest()
        {
            var customerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _leadService.CreateLeadAsync(customerId);
            return Ok(ResponseWrapper<LeadResponseDto>.SuccessResponse(result, "Lead created. AI Chat initialized."));
        }

        [HttpPost("chat/{leadId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Chat(Guid leadId, [FromBody] string message)
        {
            var result = await _leadService.ProcessChatMessageAsync(leadId, message);
            return Ok(ResponseWrapper<LeadResponseDto>.SuccessResponse(result, "Message processed by AI."));
        }
    }
}
