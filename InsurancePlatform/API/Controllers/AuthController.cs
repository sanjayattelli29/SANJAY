using Application.DTOs;
using Application.Interfaces; // service boundaries
using Microsoft.AspNetCore.Mvc; // api base

namespace API.Controllers
{
    // this handles signup and login for everyone
    [ApiController]
    [Route("api/[controller]")] // api address
    public class AuthController : ControllerBase // login/signup handler
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService; // set security service
        }

        // customer uses this to join the platform
        [HttpPost("register")] // post call for new users
        public async Task<IActionResult> Register([FromBody] RegisterCustomerDto registerDto) // receives registration json
        {
            var result = await _authService.RegisterCustomerAsync(registerDto); // run signup logic
            if (result.Status == "Error") // if it failed
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        // everyone uses this with their email and password to enter
        [HttpPost("login")] // post call for enter
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto) // receives login json
        {
            var result = await _authService.LoginAsync(loginDto); // run login check
            if (result.Status == "Error") // check return status
            {
                return Unauthorized(result);
            }
            return Ok(result);
        }
    }
}
// end of auth controller logic
