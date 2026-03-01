using System.ComponentModel.DataAnnotations; // for input validation

namespace Application.DTOs // dto folder
{
    // this class is for admin to create a new agent
    public class CreateAgentDto
    {
        // name of the agent
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // agent full name

        // email id for agent login
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty; // unique email

        // login password
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty; // login secret

        // bank account where agent gets salary
        [Required]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Bank account number must be 16 digits.")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Bank account number must be 16 digits.")]
        public string BankAccountNumber { get; set; } = string.Empty; // 16 digit number
    }
}
// agent creation data end
