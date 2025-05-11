using System.Threading.Tasks;
using Aigent.Api.Models;

namespace Aigent.Api.Interfaces
{
    /// <summary>
    /// Interface for user service
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Authenticates a user
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>Authenticated user or null</returns>
        Task<UserDto> AuthenticateAsync(string username, string password);

        /// <summary>
        /// Gets a user by ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User data or null</returns>
        Task<UserDto> GetUserAsync(string userId);

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="user">User data</param>
        /// <param name="password">Password</param>
        /// <returns>Created user</returns>
        Task<UserDto> CreateUserAsync(UserDto user, string password);

        /// <summary>
        /// Updates a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="user">Updated user data</param>
        /// <returns>Updated user</returns>
        Task<UserDto> UpdateUserAsync(string userId, UserDto user);

        /// <summary>
        /// Deletes a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if the user was deleted</returns>
        Task<bool> DeleteUserAsync(string userId);
        
        /// <summary>
        /// Validates a user's credentials
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>True if the credentials are valid</returns>
        Task<bool> ValidateCredentialsAsync(string username, string password);
    }
}
