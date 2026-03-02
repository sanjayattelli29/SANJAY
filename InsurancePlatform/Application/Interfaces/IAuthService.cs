using Application.DTOs;

namespace Application.Interfaces
{
    // this interface lists all the things about user account management
    public interface IAuthService
    {
        // register a new customer in system
        Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerDto registerDto);

        // check user password and login
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);

        // create a new agent login (only for admin)
        Task<object> CreateAgentAsync(CreateAgentDto agentDto);

        // create a new claim officer (only for admin)
        Task<object> CreateClaimOfficerAsync(CreateClaimOfficerDto claimOfficerDto);

        // get all users who have a specific role
        Task<IEnumerable<object>> GetUsersByRoleAsync(string role);

        // get every single user in system
        Task<IEnumerable<object>> GetAllUsersAsync();

        // delete a user from system
        Task<object> DeleteUserAsync(string userId);
    }
}
