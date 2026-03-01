using System.ComponentModel.DataAnnotations; // validation rules

namespace Application.DTOs // data transfer folder
{
    // this class is for admin to create a new officer who checks claims
    public class CreateClaimOfficerDto
    {
        // name of the officer
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // officer name

        // email id for login
        [Required]
        [EmailAddress]
        public string EmailId { get; set; } = string.Empty; // work email

        // login password
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty; // private password

        // bank account for salary
        [Required]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Bank account number must be 16 digits.")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Bank account number must be 16 digits.")]
        public string BankAccountNumber { get; set; } = string.Empty; // 16 digit bank id
    }
}
// claim officer data end
