using InsurancePlatform.DTOs.Auth;

namespace InsurancePlatform.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterCustomerAsync(RegisterCustomerDto dto);
        Task<string> LoginAsync(LoginDto dto);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    }
}