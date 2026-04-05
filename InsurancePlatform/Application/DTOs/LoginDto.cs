using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    /// <summary>
    /// This class is used when a user (Customer, Agent, or Admin) tries to log into the website.
    /// It simple holds their email and password.
    /// </summary>
    public class LoginDto
    {
        // The email address the user used when they signed up
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;

        // The secret password for the user's account
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
