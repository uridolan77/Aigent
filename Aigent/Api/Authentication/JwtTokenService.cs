using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Aigent.Api.Authentication
{
    /// <summary>
    /// JWT implementation of ITokenService
    /// </summary>
    public class JwtTokenService : ITokenService
    {
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;

        /// <summary>
        /// Initializes a new instance of the JwtTokenService class
        /// </summary>
        /// <param name="configuration">Configuration for the service</param>
        public JwtTokenService(IConfiguration configuration)
        {
            _secret = configuration["Aigent:Api:JwtSecret"] ?? throw new ArgumentNullException("JwtSecret configuration is missing");
            _issuer = configuration["Aigent:Api:JwtIssuer"];
            _audience = configuration["Aigent:Api:JwtAudience"];
            
            if (!int.TryParse(configuration["Aigent:Api:JwtExpirationMinutes"], out _expirationMinutes))
            {
                _expirationMinutes = 60; // Default to 60 minutes
            }
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
            return JwtTokenHandler.GenerateToken(
                userId,
                username,
                roles,
                _secret,
                _issuer,
                _audience,
                _expirationMinutes);
        }

        /// <summary>
        /// Validates a token
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>Whether the token is valid</returns>
        public bool ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = JwtTokenHandler.GetTokenValidationParameters(_secret, _issuer, _audience);

            try
            {
                tokenHandler.ValidateToken(token, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
