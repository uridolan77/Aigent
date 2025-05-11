using System;
using System.Threading.Tasks;
using Aigent.Api.Interfaces;
using Aigent.Api.Models;

namespace Aigent.Api.Compatibility
{
    /// <summary>
    /// Adapter for IUserService to legacy Authentication.IUserService
    /// </summary>
    public class UserServiceAdapter : Authentication.IUserService
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;

        /// <summary>
        /// Initializes a new instance of the UserServiceAdapter class
        /// </summary>
        /// <param name="userService">User service</param>
        /// <param name="tokenService">Token service</param>
        public UserServiceAdapter(IUserService userService, ITokenService tokenService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        /// <summary>
        /// Authenticates a user
        /// </summary>
        /// <param name="username">Username of the user</param>
        /// <param name="password">Password of the user</param>
        /// <returns>Authentication result</returns>
        public Task<AuthResult> AuthenticateAsync(string username, string password)
        {
            var user = _userService.AuthenticateAsync(username, password).GetAwaiter().GetResult();
            
            if (user == null)
            {
                return Task.FromResult(new AuthResult
                {
                    Success = false,
                    Message = "Invalid username or password"
                });
            }
            
            var tokenResult = _tokenService.GenerateTokenAsync(user).GetAwaiter().GetResult();
            
            return Task.FromResult(new AuthResult
            {
                Success = true,
                Token = tokenResult.Token,
                User = user
            });
        }

        /// <summary>
        /// Gets a user by ID
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <returns>The user</returns>
        public Task<UserDto> GetUserAsync(string userId)
        {
            return _userService.GetUserAsync(userId);
        }
    }
}
