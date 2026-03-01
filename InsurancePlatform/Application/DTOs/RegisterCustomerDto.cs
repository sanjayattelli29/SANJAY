using System.ComponentModel.DataAnnotations; // validation namespace

namespace Application.DTOs // dto directory
{
    // this class is for new customers to sign up
    public class RegisterCustomerDto
    {
        // customer's full name
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // customer name

        // email for login and updates
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty; // registration email

        // secret password for account
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty; // account password

        // mobile number for contact
        [Required]
        [Phone]
        public string MobileNumber { get; set; } = string.Empty; // phone number
    }
}
// registration data ends
