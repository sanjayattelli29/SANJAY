using InsurancePlatform.Models;

namespace InsurancePlatform.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}