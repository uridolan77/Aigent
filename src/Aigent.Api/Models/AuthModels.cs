namespace Aigent.Api.Models
{
    /// <summary>
    /// Login request model
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Username of the user
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Password of the user
        /// </summary>
        public string Password { get; set; }
    }

    /// <summary>
    /// Authentication result model
    /// </summary>
    public class AuthResult
    {
        /// <summary>
        /// Whether the authentication was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Authentication token
        /// </summary>
        public string Token { get; set; }
        
        /// <summary>
        /// User information
        /// </summary>
        public UserDto User { get; set; }
        
        /// <summary>
        /// Error message if authentication failed
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// User data transfer object
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// ID of the user
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Username of the user
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Roles of the user
        /// </summary>
        public string[] Roles { get; set; }
    }
}
