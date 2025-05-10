using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Aigent.Api.Authentication;
using Aigent.Api.Models;

namespace Aigent.Api.Controllers
{
    /// <summary>
    /// Controller for authentication
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        /// <summary>
        /// Initializes a new instance of the AuthController class
        /// </summary>
        /// <param name="userService">User service</param>
        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Logs in a user
        /// </summary>
        /// <param name="request">Login request</param>
        /// <returns>Authentication result</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResult>>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(ApiResponse<AuthResult>.Error("Username and password are required"));
            }

            var result = await _userService.AuthenticateAsync(request.Username, request.Password);

            if (!result.Success)
            {
                return Unauthorized(ApiResponse<AuthResult>.Error(result.Message));
            }

            return Ok(ApiResponse<AuthResult>.Ok(result));
        }

        /// <summary>
        /// Gets the current user
        /// </summary>
        /// <returns>User information</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            var userId = User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<UserDto>.Error("User not authenticated"));
            }

            var user = await _userService.GetUserAsync(userId);

            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.Error("User not found"));
            }

            return Ok(ApiResponse<UserDto>.Ok(user));
        }
    }
}
