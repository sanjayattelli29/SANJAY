using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    // this class is for new customers to sign up
    public class RegisterCustomerDto
    {
        // customer's full name
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // email for login and updates
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;

        // secret password for account
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        // mobile number for contact
        [Required]
        [Phone]
        public string MobileNumber { get; set; } = string.Empty;
    }
}
