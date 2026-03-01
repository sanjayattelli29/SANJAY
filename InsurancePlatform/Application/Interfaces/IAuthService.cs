using Application.DTOs; // import data objects

namespace Application.Interfaces // interface folder
{
    // this interface lists all the things about user account management
    public interface IAuthService // authentication service interface
    {
        // register a new customer in system
        Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerDto registerDto);

        // check user password and login
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto); // returns jwt token

        // create a new agent login (only for admin)
        Task<object> CreateAgentAsync(CreateAgentDto agentDto); // admin only task

        // create a new claim officer (only for admin)
        Task<object> CreateClaimOfficerAsync(CreateClaimOfficerDto claimOfficerDto); // admin only task

        // get all users who have a specific role
        Task<IEnumerable<object>> GetUsersByRoleAsync(string role); // filter users

        // get every single user in system
        Task<IEnumerable<object>> GetAllUsersAsync();

        // delete a user from system
        Task<object> DeleteUserAsync(string userId); // remove user account
    }
}
// auth service interface ends
