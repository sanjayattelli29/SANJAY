using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
// sample
namespace Infrastructure.Services
{
    /// <summary>
    /// This service handles the creation of "Digital ID Cards" (JWT Tokens).
    /// When a user logs in, we give them this token so the system knows who they are.
    /// </summary>
    public class JwtTokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        // Constructor sets up access to our secret security settings.
        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// This method builds a new security token for a user.
        /// </summary>
        public JwtSecurityToken CreateToken(List<Claim> authClaims)
        {
            // We use a "Secret Key" to sign the token so no one can fake it.
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]!));

            // Create the token with info like: Who issued it, who is it for, and when does it expire?
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["JWT:ExpiryMinutes"])),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256) // Use a strong lock.
                );

            return token;
        }

        /// <summary>
        /// This helper method turns the token object into a long text string of letters and numbers.
        /// </summary>
        public string WriteToken(JwtSecurityToken token)
        {
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
