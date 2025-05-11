using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Aigent.Api.Interfaces;
using Aigent.Api.Models;

namespace Aigent.Api.Services
{
    /// <summary>
    /// JWT implementation of ITokenService
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;
        private readonly int _refreshTokenExpirationDays;
        private readonly Dictionary<string, RefreshTokenInfo> _refreshTokens = new();

        /// <summary>
        /// Initializes a new instance of the TokenService class
        /// </summary>
        /// <param name="configuration">Configuration for the service</param>
        public TokenService(IConfiguration configuration)
        {
            _secret = configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key configuration is missing");
            _issuer = configuration["Jwt:Issuer"];
            _audience = configuration["Jwt:Audience"];
            
            if (!int.TryParse(configuration["Jwt:ExpirationMinutes"], out _expirationMinutes))
            {
                _expirationMinutes = 60; // Default to 60 minutes
            }

            if (!int.TryParse(configuration["Jwt:RefreshTokenExpirationDays"], out _refreshTokenExpirationDays))
            {
                _refreshTokenExpirationDays = 7; // Default to 7 days
            }
        }

        /// <summary>
        /// Generates a JWT token for a user
        /// </summary>
        /// <param name="user">User data</param>
        /// <returns>JWT token data</returns>
        public Task<TokenDto> GenerateTokenAsync(UserDto user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddMinutes(_expirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expiration,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken(user.Id);

            return Task.FromResult(new TokenDto
            {
                Token = tokenString,
                RefreshToken = refreshToken,
                Expiration = expiration
            });
        }

        /// <summary>
        /// Validates a JWT token
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>True if the token is valid</returns>
        public Task<bool> ValidateTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetTokenValidationParameters();

            try
            {
                tokenHandler.ValidateToken(token, validationParameters, out _);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Refreshes a JWT token
        /// </summary>
        /// <param name="token">Token to refresh</param>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>New JWT token data</returns>
        public async Task<TokenDto> RefreshTokenAsync(string token, string refreshToken)
        {
            var principal = GetClaimsFromToken(token);
            if (principal == null)
            {
                throw new SecurityTokenException("Invalid token");
            }

            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new SecurityTokenException("Invalid token");
            }

            // Check if refresh token is valid
            if (!_refreshTokens.TryGetValue(refreshToken, out var refreshTokenInfo) || 
                refreshTokenInfo.UserId != userId || 
                refreshTokenInfo.ExpirationDate < DateTime.UtcNow)
            {
                throw new SecurityTokenException("Invalid refresh token");
            }

            // Create a new token
            var username = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

            var user = new UserDto
            {
                Id = userId,
                Username = username,
                Roles = roles
            };

            // Revoke the current refresh token
            await RevokeRefreshTokenAsync(refreshToken);

            // Generate a new token
            return await GenerateTokenAsync(user);
        }

        /// <summary>
        /// Gets claims from a JWT token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>Claims principal</returns>
        public ClaimsPrincipal GetClaimsFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetTokenValidationParameters(false);

            try
            {
                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Revokes a refresh token
        /// </summary>
        /// <param name="refreshToken">Refresh token to revoke</param>
        /// <returns>True if the token was revoked</returns>
        public Task<bool> RevokeRefreshTokenAsync(string refreshToken)
        {
            var removed = _refreshTokens.Remove(refreshToken);
            return Task.FromResult(removed);
        }

        private string GenerateRefreshToken(string userId)
        {
            var refreshToken = Guid.NewGuid().ToString();
            var expirationDate = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);

            _refreshTokens[refreshToken] = new RefreshTokenInfo
            {
                UserId = userId,
                ExpirationDate = expirationDate
            };

            return refreshToken;
        }

        private TokenValidationParameters GetTokenValidationParameters(bool validateLifetime = true)
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
                ValidateIssuer = !string.IsNullOrEmpty(_issuer),
                ValidIssuer = _issuer,
                ValidateAudience = !string.IsNullOrEmpty(_audience),
                ValidAudience = _audience,
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.Zero
            };
        }

        private class RefreshTokenInfo
        {
            public string UserId { get; set; }
            public DateTime ExpirationDate { get; set; }
        }
    }
}
