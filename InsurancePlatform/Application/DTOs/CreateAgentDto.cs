using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    // this class is for admin to create a new agent
    public class CreateAgentDto
    {
        // name of the agent
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // email id for agent login
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty;

        // login password
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        // bank account where agent gets salary
        [Required]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Bank account number must be 16 digits.")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Bank account number must be 16 digits.")]
        public string BankAccountNumber { get; set; } = string.Empty;
    }
}
