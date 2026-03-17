using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Services
{
    // this class does the actual work for login and registering users
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IVapiService _vapiService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IVapiService vapiService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _vapiService = vapiService;
        }

        // code to create a new customer account
        public async Task<AuthResponseDto> RegisterCustomerAsync(RegisterCustomerDto registerDto)
        {
            // check if someone already uses this email
            var userExists = await _userManager.FindByEmailAsync(registerDto.EmailId);
            if (userExists != null)
                return new AuthResponseDto { Status = "Error", Message = "User already exists!" };

            // mapping dto to user object
            ApplicationUser user = new()
            {
                Email = registerDto.EmailId,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = registerDto.EmailId,
                FullName = registerDto.Name,
                PhoneNumber = registerDto.MobileNumber
            };

            // try to save user to database
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "User creation failed! Please check user details and try again." };

            // making sure customer role exists
            if (!await _roleManager.RoleExistsAsync(UserRoles.Customer))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Customer));

            // add user to customer role
            await _userManager.AddToRoleAsync(user, UserRoles.Customer);

            // Trigger AI Agent Welcome Call after 90 seconds
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(90));
                    await _vapiService.TriggerWelcomeCallAsync(user.PhoneNumber!, user.FullName!);
                }
                catch (Exception ex)
                {
                    // Log error or handle silently for now
                    Console.WriteLine($"Vapi Call Trigger failed: {ex.Message}");
                }
            });

            return new AuthResponseDto { Status = "Success", Message = "Customer created successfully!" };
        }

        // code to check login info and give token
        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // find user by email
            var user = await _userManager.FindByEmailAsync(loginDto.EmailId);
            // check if user exists and password is correct
            if (user != null && await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email!),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = CreateToken(authClaims);

                return new AuthResponseDto
                {
                    Status = "Success",
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    Expiration = token.ValidTo,
                    Role = userRoles.FirstOrDefault(),
                    Email = user.Email,
                    Id = user.Id,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    IsKycVerified = user.IsKycVerified,
                    ProfileImageUrl = user.ProfileImageUrl
                };
            }
            return new AuthResponseDto { Status = "Error", Message = "Invalid login attempt." };
        }

        // code to create a new agent with login access
        public async Task<AuthResponseDto> CreateAgentAsync(CreateAgentDto agentDto)
        {
            // check if this email is already taken by someone else
            var userExists = await _userManager.FindByEmailAsync(agentDto.EmailId);
            if (userExists != null)
                return new AuthResponseDto { Status = "Error", Message = "Agent already exists!" };

            // create the agent user object
            ApplicationUser user = new()
            {
                Email = agentDto.EmailId,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = agentDto.EmailId,
                FullName = agentDto.Name,
                BankAccountNumber = agentDto.BankAccountNumber,
                InitialPassword = agentDto.Password
            };

            // try to save to database with password
            var result = await _userManager.CreateAsync(user, agentDto.Password);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "Agent creation failed!" };

            // make sure agent role exists in the system
            if (!await _roleManager.RoleExistsAsync(UserRoles.Agent))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Agent));

            // assign agent role to this user
            await _userManager.AddToRoleAsync(user, UserRoles.Agent);

            return new AuthResponseDto { Status = "Success", Message = "Agent created successfully!" };
        }

        // code to create a new claim officer with login access
        public async Task<AuthResponseDto> CreateClaimOfficerAsync(CreateClaimOfficerDto claimOfficerDto)
        {
            // check if this email is already registered
            var userExists = await _userManager.FindByEmailAsync(claimOfficerDto.EmailId);
            if (userExists != null)
                return new AuthResponseDto { Status = "Error", Message = "Claim Officer already exists!" };

            // build the claim officer user object
            ApplicationUser user = new()
            {
                Email = claimOfficerDto.EmailId,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = claimOfficerDto.EmailId,
                FullName = claimOfficerDto.Name,
                BankAccountNumber = claimOfficerDto.BankAccountNumber,
                InitialPassword = claimOfficerDto.Password
            };

            // save user with password to database
            var result = await _userManager.CreateAsync(user, claimOfficerDto.Password);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "Claim Officer creation failed!" };

            // create claim officer role if it doesn't exist yet
            if (!await _roleManager.RoleExistsAsync(UserRoles.ClaimOfficer))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.ClaimOfficer));

            // give claim officer permissions to this user
            await _userManager.AddToRoleAsync(user, UserRoles.ClaimOfficer);

            return new AuthResponseDto { Status = "Success", Message = "Claim Officer created successfully!" };
        }

        // get a list of all users who have a specific role like agent or officer
        public async Task<IEnumerable<UserListingDto>> GetUsersByRoleAsync(string role)
        {
            // fetch users from database who have this role
            var users = await _userManager.GetUsersInRoleAsync(role);
            // return only the information we need
            return users.Select(u => new UserListingDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                BankAccountNumber = u.BankAccountNumber
            });
        }

        // get a full list of everyone registered in the system with their jobs
        public async Task<IEnumerable<UserListingDto>> GetAllUsersAsync()
        {
            // get all users from the database
            var users = _userManager.Users.ToList();
            var result = new List<UserListingDto>();

            // for each user also find what role they have
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserListingDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    BankAccountNumber = user.BankAccountNumber,
                    Roles = roles,
                    Role = roles.FirstOrDefault() ?? "Partner", // Fallback for display
                    CreatedDate = user.CreatedAt,
                    InitialPassword = user.InitialPassword
                });
            }

            return result;
        }

        // completely remove a user from our database
        public async Task<AuthResponseDto> DeleteUserAsync(string userId)
        {
            // first check if user exists
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new AuthResponseDto { Status = "Error", Message = "User not found!" };

            // try to delete from database
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "User deletion failed!" };

            return new AuthResponseDto { Status = "Success", Message = "User deleted successfully!" };
        }

        public async Task<AuthResponseDto> CompleteKycAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new AuthResponseDto { Status = "Error", Message = "User not found!" };

            user.IsKycVerified = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "Failed to update KYC status!" };

            return new AuthResponseDto { Status = "Success", Message = "KYC Verified!" };
        }

        // save the ImageKit profile image URL against the user record
        public async Task<AuthResponseDto> UpdateProfileImageAsync(string userId, string imageUrl)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return new AuthResponseDto { Status = "Error", Message = "User not found!" };

            user.ProfileImageUrl = imageUrl;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return new AuthResponseDto { Status = "Error", Message = "Failed to update profile image!" };

            return new AuthResponseDto { Status = "Success", Message = "Profile image updated!", ProfileImageUrl = imageUrl };
        }

        // this creates the security token that proves user is logged in
        private JwtSecurityToken CreateToken(List<System.Security.Claims.Claim> authClaims)
        {
            // get the secret key from settings file
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]!));

            // build the token with expiry time and user info
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JWT:ExpiryMinutes"])),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }
    }
}
