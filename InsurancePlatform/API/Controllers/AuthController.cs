using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces;

namespace API.Controllers
{
    // this handles signup and login for everyone
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly IFileStorageService _fileStorageService;
        public AuthController(IIdentityService identityService, IFileStorageService fileStorageService)
        {
            _identityService = identityService;
            _fileStorageService = fileStorageService;
        }

        // customer uses this to join the platform
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCustomerDto registerDto)
        {
            var result = await _identityService.RegisterCustomerAsync(registerDto);
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
            var result = await _identityService.LoginAsync(loginDto);
            if (result.Status == "Error")
            {
                return Unauthorized(result);
            }
            return Ok(result);
        }

        // mark user as kyc verified
        [HttpPost("complete-kyc/{userId}")]
        public async Task<IActionResult> CompleteKyc(string userId)
        {
            var result = await _identityService.CompleteKycAsync(userId);
            if (result.Status == "Error")
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        // upload a profile photo to ImageKit and save the url in the database
        [HttpPost("upload-profile-image")]
        [Authorize]
        public async Task<IActionResult> UploadProfileImage([FromBody] UploadProfileImageDto dto)
        {
            try
            {
                // strip the data:image/...;base64, prefix if present
                var base64Data = dto.Base64Image.Contains(",")
                    ? dto.Base64Image.Substring(dto.Base64Image.IndexOf(',') + 1)
                    : dto.Base64Image;

                var imageBytes = Convert.FromBase64String(base64Data);
                using var stream = new MemoryStream(imageBytes);

                // upload to ImageKit in /profile-images folder
                var uploadResult = await _fileStorageService.UploadFileAsync(stream, dto.FileName, "/profile-images");

                // save the CDN url to the user record
                var updateResult = await _identityService.UpdateProfileImageAsync(dto.UserId, uploadResult.FileUrl);
                if (updateResult.Status == "Error")
                    return BadRequest(updateResult);

                return Ok(new { imageUrl = uploadResult.FileUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Upload failed: {ex.Message}" });
            }
        }

        // force reset a user password (bypasses reset token workflow)
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _identityService.ResetPasswordAsync(dto.Email, dto.NewPassword);
            if (result.Status == "Error")
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
    }

    // DTO for profile image upload request
    public class UploadProfileImageDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Base64Image { get; set; } = string.Empty;
        public string FileName { get; set; } = "profile.jpg";
    }
}
