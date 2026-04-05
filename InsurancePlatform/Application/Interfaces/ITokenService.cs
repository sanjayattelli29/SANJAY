using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Application.Interfaces
{
    /// <summary>
    /// This interface handles the creation of digital "Security Badges" (JWT Tokens).
    /// These tokens prove who a user is so they don't have to log in on every single click.
    /// </summary>
    public interface ITokenService
    {
        // Create the security token based on the user's information (like their name and role)
        JwtSecurityToken CreateToken(List<Claim> authClaims);
        
        // Convert the token object into a long string of text that can be sent to the browser
        string WriteToken(JwtSecurityToken token);
    }
}
