using System;
using System.Collections.Generic;
using InsurancePlatform.Models.Base;

namespace InsurancePlatform.Models
{
    public class User : BaseEntity
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string PasswordHash { get; set; }

        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsApproved { get; set; } = false;
        public bool RequiresPasswordChange { get; set; } = false;

        public DateTime? LastLoginAt { get; set; }

        // Navigation
        public CustomerProfile CustomerProfile { get; set; }
        public AgentProfile AgentProfile { get; set; }
        public ClaimsOfficerProfile ClaimsOfficerProfile { get; set; }
    }

    public enum UserRole
    {
        Customer = 1,
        Agent = 2,
        ClaimsOfficer = 3,
        Admin = 4
    }
}