using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Aigent.Api.Authentication
{
    /// <summary>
    /// Handles JWT token generation and validation
    /// </summary>
    public static class JwtTokenHandler
    {
        /// <summary>
        /// Generates a JWT token
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <param name="username">Username of the user</param>
        /// <param name="roles">Roles of the user</param>
        /// <param name="secret">Secret key for signing the token</param>
        /// <param name="issuer">Issuer of the token</param>
        /// <param name="audience">Audience of the token</param>
        /// <param name="expirationMinutes">Expiration time in minutes</param>
        /// <returns>The generated JWT token</returns>
        public static string GenerateToken(
            string userId,
            string username,
            IEnumerable<string> roles,
            string secret,
            string issuer,
            string audience,
            int expirationMinutes = 60)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Gets token validation parameters
        /// </summary>
        /// <param name="secret">Secret key for validating the token</param>
        /// <param name="issuer">Issuer of the token</param>
        /// <param name="audience">Audience of the token</param>
        /// <returns>Token validation parameters</returns>
        public static TokenValidationParameters GetTokenValidationParameters(
            string secret,
            string issuer,
            string audience)
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = !string.IsNullOrEmpty(issuer),
                ValidIssuer = issuer,
                ValidateAudience = !string.IsNullOrEmpty(audience),
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }
    }
}
