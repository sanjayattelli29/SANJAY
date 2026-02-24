using InsurancePlatform.DTOs.Auth;
using InsurancePlatform.Helpers;
using InsurancePlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InsurancePlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register-customer")]
        public async Task<IActionResult> RegisterCustomer(RegisterCustomerDto dto)
        {
            var token = await _authService.RegisterCustomerAsync(dto);
            return Ok(ResponseWrapper<string>.SuccessResponse(token, "Customer registered successfully"));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var token = await _authService.LoginAsync(dto);
            return Ok(ResponseWrapper<string>.SuccessResponse(token, "Login successful"));
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _authService.ChangePasswordAsync(userId, dto);
            if (result)
                return Ok(ResponseWrapper<bool>.SuccessResponse(true, "Password changed successfully"));

            return BadRequest(ResponseWrapper<bool>.ErrorResponse("Password change failed"));
        }
    }
}