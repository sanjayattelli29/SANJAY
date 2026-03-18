using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Application.Interfaces.Infrastructure
{
    public interface ITokenService
    {
        JwtSecurityToken CreateToken(List<Claim> authClaims);
        string WriteToken(JwtSecurityToken token);
    }
}
