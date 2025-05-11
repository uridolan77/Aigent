using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Aigent.Api.Models;

namespace Aigent.Api.Interfaces
{
    /// <summary>
    /// Interface for JWT token service
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a JWT token for a user
        /// </summary>
        /// <param name="user">User data</param>
        /// <returns>JWT token data</returns>
        Task<TokenDto> GenerateTokenAsync(UserDto user);

        /// <summary>
        /// Validates a JWT token
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>True if the token is valid</returns>
        Task<bool> ValidateTokenAsync(string token);

        /// <summary>
        /// Refreshes a JWT token
        /// </summary>
        /// <param name="token">Token to refresh</param>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>New JWT token data</returns>
        Task<TokenDto> RefreshTokenAsync(string token, string refreshToken);

        /// <summary>
        /// Gets claims from a JWT token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>Claims principal</returns>
        ClaimsPrincipal GetClaimsFromToken(string token);
        
        /// <summary>
        /// Revokes a refresh token
        /// </summary>
        /// <param name="refreshToken">Refresh token to revoke</param>
        /// <returns>True if the token was revoked</returns>
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
    }
}
