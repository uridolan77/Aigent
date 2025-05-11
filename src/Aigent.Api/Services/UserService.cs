using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Aigent.Api.Interfaces;
using Aigent.Api.Models;

namespace Aigent.Api.Services
{
    /// <summary>
    /// Implementation of IUserService
    /// </summary>
    public class UserService : IUserService
    {
        private readonly Dictionary<string, User> _usersById = new();
        private readonly Dictionary<string, User> _usersByUsername = new();

        /// <summary>
        /// Initializes a new instance of the UserService class
        /// </summary>
        /// <param name="configuration">Configuration for the service</param>
        public UserService(IConfiguration configuration)
        {
            // In a real application, users would be stored in a database
            // For this example, we'll use in-memory collections
            AddUser(new User
            {
                Id = "1",
                Username = "admin",
                PasswordHash = HashPassword("admin123"),
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                Roles = new[] { "Admin" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            AddUser(new User
            {
                Id = "2",
                Username = "user",
                PasswordHash = HashPassword("user123"),
                Email = "user@example.com",
                FirstName = "Regular",
                LastName = "User",
                Roles = new[] { "User" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Authenticates a user
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>Authenticated user or null</returns>
        public Task<UserDto> AuthenticateAsync(string username, string password)
        {
            if (!_usersByUsername.TryGetValue(username, out var user))
            {
                return Task.FromResult<UserDto>(null);
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                return Task.FromResult<UserDto>(null);
            }

            if (!user.IsActive)
            {
                return Task.FromResult<UserDto>(null);
            }

            return Task.FromResult(MapToDto(user));
        }

        /// <summary>
        /// Gets a user by ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User data or null</returns>
        public Task<UserDto> GetUserAsync(string userId)
        {
            if (!_usersById.TryGetValue(userId, out var user))
            {
                return Task.FromResult<UserDto>(null);
            }

            return Task.FromResult(MapToDto(user));
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="user">User data</param>
        /// <param name="password">Password</param>
        /// <returns>Created user</returns>
        public Task<UserDto> CreateUserAsync(UserDto userDto, string password)
        {
            if (string.IsNullOrEmpty(userDto.Username))
            {
                throw new ArgumentException("Username is required", nameof(userDto.Username));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password is required", nameof(password));
            }

            if (_usersByUsername.ContainsKey(userDto.Username))
            {
                throw new InvalidOperationException($"User with username '{userDto.Username}' already exists");
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = userDto.Username,
                PasswordHash = HashPassword(password),
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Roles = userDto.Roles ?? new[] { "User" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            AddUser(user);

            return Task.FromResult(MapToDto(user));
        }

        /// <summary>
        /// Updates a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="user">Updated user data</param>
        /// <returns>Updated user</returns>
        public Task<UserDto> UpdateUserAsync(string userId, UserDto userDto)
        {
            if (!_usersById.TryGetValue(userId, out var user))
            {
                throw new KeyNotFoundException($"User with ID '{userId}' not found");
            }

            // If username is changing, check for conflicts
            if (userDto.Username != user.Username && _usersByUsername.ContainsKey(userDto.Username))
            {
                throw new InvalidOperationException($"User with username '{userDto.Username}' already exists");
            }

            // Remove old username mapping
            _usersByUsername.Remove(user.Username);

            // Update user
            user.Username = userDto.Username ?? user.Username;
            user.Email = userDto.Email ?? user.Email;
            user.FirstName = userDto.FirstName ?? user.FirstName;
            user.LastName = userDto.LastName ?? user.LastName;
            user.Roles = userDto.Roles ?? user.Roles;
            user.UpdatedAt = DateTime.UtcNow;

            // Add with new username
            _usersByUsername[user.Username] = user;

            return Task.FromResult(MapToDto(user));
        }

        /// <summary>
        /// Deletes a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if the user was deleted</returns>
        public Task<bool> DeleteUserAsync(string userId)
        {
            if (!_usersById.TryGetValue(userId, out var user))
            {
                return Task.FromResult(false);
            }

            _usersById.Remove(userId);
            _usersByUsername.Remove(user.Username);

            return Task.FromResult(true);
        }
        
        /// <summary>
        /// Validates a user's credentials
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>True if the credentials are valid</returns>
        public Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            if (!_usersByUsername.TryGetValue(username, out var user))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(VerifyPassword(password, user.PasswordHash) && user.IsActive);
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

        private void AddUser(User user)
        {
            _usersById[user.Id] = user;
            _usersByUsername[user.Username] = user;
        }

        private UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = user.Roles,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        private class User
        {
            public string Id { get; set; }
            public string Username { get; set; }
            public string PasswordHash { get; set; }
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string[] Roles { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }
    }
}
