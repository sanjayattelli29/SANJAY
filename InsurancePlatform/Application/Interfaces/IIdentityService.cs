using Application.DTOs;

namespace Application.Interfaces
{
    // this interface lists all the things about user account management
    public interface IIdentityService
    {
        // register a new customer in system
        Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerDto registerDto);

        // check user password and login
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);

        // create a new agent login (only for admin)
        Task<AuthResponseDto> CreateAgentAsync(CreateAgentDto agentDto);

        // create a new claim officer (only for admin)
        Task<AuthResponseDto> CreateClaimOfficerAsync(CreateClaimOfficerDto claimOfficerDto);

        // get all users who have a specific role
        Task<IEnumerable<UserListingDto>> GetUsersByRoleAsync(string role);

        // get every single user in system
        Task<IEnumerable<UserListingDto>> GetAllUsersAsync();

        // delete a user from system
        Task<AuthResponseDto> DeleteUserAsync(string userId);

        Task<AuthResponseDto> CompleteKycAsync(string userId);

        // update the user's profile image url after uploading to ImageKit
        Task<AuthResponseDto> UpdateProfileImageAsync(string userId, string imageUrl);

        // reset password bypassing standard workflow (as verified by client OTP)
        Task<AuthResponseDto> ResetPasswordAsync(string email, string newPassword);
    }

    public class UserListingDto
    {
        public string Id { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? Role { get; set; }
        public IEnumerable<string>? Roles { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? InitialPassword { get; set; }
    }
}
