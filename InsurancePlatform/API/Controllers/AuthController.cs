using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
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

        /// <summary>
        /// Endpoint for customer registration.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCustomerDto registerDto)
        {
            var result = await _authService.RegisterCustomerAsync(registerDto);
            if (result.Status == "Error")
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Endpoint for user login (Admin, Customer, Agent, ClaimOfficer).
        /// Returns a JWT token upon successful authentication.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            if (result.Status == "Error")
            {
                return Unauthorized(result);
            }
            return Ok(result);
        }
    }
}
