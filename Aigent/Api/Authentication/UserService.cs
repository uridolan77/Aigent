using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Aigent.Api.Models;

namespace Aigent.Api.Authentication
{
    /// <summary>
    /// Implementation of IUserService
    /// </summary>
    public class UserService : IUserService
    {
        private readonly ITokenService _tokenService;
        private readonly List<User> _users;

        /// <summary>
        /// Initializes a new instance of the UserService class
        /// </summary>
        /// <param name="tokenService">Token service</param>
        /// <param name="configuration">Configuration for the service</param>
        public UserService(ITokenService tokenService, IConfiguration configuration)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            
            // In a real application, users would be stored in a database
            // For this example, we'll use a hardcoded list
            _users = new List<User>
            {
                new User
                {
                    Id = "1",
                    Username = "admin",
                    PasswordHash = HashPassword("admin123"),
                    Roles = new[] { "Admin" }
                },
                new User
                {
                    Id = "2",
                    Username = "user",
                    PasswordHash = HashPassword("user123"),
                    Roles = new[] { "User" }
                }
            };
        }

        /// <summary>
        /// Authenticates a user
        /// </summary>
        /// <param name="username">Username of the user</param>
        /// <param name="password">Password of the user</param>
        /// <returns>Authentication result</returns>
        public Task<AuthResult> AuthenticateAsync(string username, string password)
        {
            var user = _users.SingleOrDefault(u => u.Username == username);
            
            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                return Task.FromResult(new AuthResult
                {
                    Success = false,
                    Message = "Invalid username or password"
                });
            }
            
            var token = _tokenService.GenerateToken(user.Id, user.Username, user.Roles);
            
            return Task.FromResult(new AuthResult
            {
                Success = true,
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Roles = user.Roles
                }
            });
        }

        /// <summary>
        /// Gets a user by ID
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <returns>The user</returns>
        public Task<UserDto> GetUserAsync(string userId)
        {
            var user = _users.SingleOrDefault(u => u.Id == userId);
            
            if (user == null)
            {
                return Task.FromResult<UserDto>(null);
            }
            
            return Task.FromResult(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Roles = user.Roles
            });
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        private class User
        {
            public string Id { get; set; }
            public string Username { get; set; }
            public string PasswordHash { get; set; }
            public string[] Roles { get; set; }
        }
    }
}
