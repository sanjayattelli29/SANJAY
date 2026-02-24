using InsurancePlatform.DTOs.Auth;
using InsurancePlatform.Helpers;
using InsurancePlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsurancePlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpPost("create-agent")]
        public async Task<IActionResult> CreateAgent(CreateAgentDto dto)
        {
            await _adminService.CreateAgentAsync(dto);
            return Ok(ResponseWrapper<string>.SuccessResponse(null, "Agent created successfully"));
        }

        [HttpPost("create-claims-officer")]
        public async Task<IActionResult> CreateClaimsOfficer(CreateClaimsOfficerDto dto)
        {
            await _adminService.CreateClaimsOfficerAsync(dto);
            return Ok(ResponseWrapper<string>.SuccessResponse(null, "Claims Officer created successfully"));
        }
    }
}
