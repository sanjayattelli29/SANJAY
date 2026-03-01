using System.ComponentModel.DataAnnotations; // for login validation

namespace Application.DTOs // data objects folder
{
    // this class is for users when they try to login
    public class LoginDto
    {
        // user's registered email
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty; // login email

        // user's password
        [Required]
        public string Password { get; set; } = string.Empty; // user password
    }
}
// login model ends
