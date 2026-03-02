using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // this handles signup and login for everyone
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // customer uses this to join the platform
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

        // everyone uses this with their email and password to enter
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
