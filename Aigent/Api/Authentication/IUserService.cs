using System.Threading.Tasks;
using Aigent.Api.Models;

namespace Aigent.Api.Authentication
{
    /// <summary>
    /// Interface for user services
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Authenticates a user
        /// </summary>
        /// <param name="username">Username of the user</param>
        /// <param name="password">Password of the user</param>
        /// <returns>Authentication result</returns>
        Task<AuthResult> AuthenticateAsync(string username, string password);
        
        /// <summary>
        /// Gets a user by ID
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <returns>The user</returns>
        Task<UserDto> GetUserAsync(string userId);
    }
}
