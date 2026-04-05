using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    /// <summary>
    /// This class is used when a brand new user (Customer) wants to sign up for an account.
    /// It collects their basic contact information and password choice.
    /// </summary>
    public class RegisterCustomerDto
    {
        // The full name of the customer
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // The email address they will use to log in (must be unique)
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;

        // The secure password they want for their new account
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        // Their phone number so we can call them about their policies
        [Required]
        [Phone]
        public string MobileNumber { get; set; } = string.Empty;
    }
}
