using System.Collections.Generic;

namespace Aigent.Api.Authentication
{
    /// <summary>
    /// Interface for token services
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a token for a user
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <param name="username">Username of the user</param>
        /// <param name="roles">Roles of the user</param>
        /// <returns>The generated token</returns>
        string GenerateToken(string userId, string username, IEnumerable<string> roles);
        
        /// <summary>
        /// Validates a token
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>Whether the token is valid</returns>
        bool ValidateToken(string token);
    }
}
