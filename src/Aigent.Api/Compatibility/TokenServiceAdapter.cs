using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aigent.Api.Interfaces;

namespace Aigent.Api.Compatibility
{
    /// <summary>
    /// Adapter for ITokenService to legacy Authentication.ITokenService
    /// </summary>
    public class TokenServiceAdapter : Authentication.ITokenService
    {
        private readonly ITokenService _tokenService;

        /// <summary>
        /// Initializes a new instance of the TokenServiceAdapter class
        /// </summary>
        /// <param name="tokenService">Token service</param>
        public TokenServiceAdapter(ITokenService tokenService)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        /// <summary>
        /// Generates a token for a user
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <param name="username">Username of the user</param>
        /// <param name="roles">Roles of the user</param>
        /// <returns>The generated token</returns>
        public string GenerateToken(string userId, string username, IEnumerable<string> roles)
        {
            var user = new Models.UserDto
            {
                Id = userId,
                Username = username,
                Roles = roles != null ? new List<string>(roles).ToArray() : new string[0]
            };

            var tokenResult = _tokenService.GenerateTokenAsync(user).GetAwaiter().GetResult();
            return tokenResult.Token;
        }

        /// <summary>
        /// Validates a token
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>Whether the token is valid</returns>
        public bool ValidateToken(string token)
        {
            return _tokenService.ValidateTokenAsync(token).GetAwaiter().GetResult();
        }
    }
}
