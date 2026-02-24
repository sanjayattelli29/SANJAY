using System;

namespace InsurancePlatform.DTOs.Auth
{
    public class RegisterCustomerDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string AadhaarNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? OccupationType { get; set; }
        public decimal? AnnualIncome { get; set; }
    }
}