using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    // this class is for users when they try to login
    public class LoginDto
    {
        // user's registered email
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;

        // user's password
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
