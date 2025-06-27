using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace Masark.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet("users")]
        [Authorize]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = new object[0];

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get users",
                    message = ex.Message
                });
            }
        }

        [HttpPost("users")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserBasicRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid request data",
                        details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var user = new
                {
                    id = new Random().Next(1000, 9999),
                    username = request.Username,
                    email = request.Email,
                    created_at = DateTime.UtcNow.ToString("O"),
                    updated_at = DateTime.UtcNow.ToString("O")
                };

                return StatusCode(201, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to create user",
                    message = ex.Message
                });
            }
        }

        [HttpGet("users/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUser(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid user ID"
                    });
                }

                return NotFound(new
                {
                    success = false,
                    error = "User not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get user",
                    message = ex.Message
                });
            }
        }

        [HttpPut("users/{userId}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid user ID"
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid request data",
                        details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var user = new
                {
                    id = userId,
                    username = request.Username,
                    email = request.Email,
                    updated_at = DateTime.UtcNow.ToString("O")
                };

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to update user",
                    message = ex.Message
                });
            }
        }

        [HttpDelete("users/{userId}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid user ID"
                    });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to delete user",
                    message = ex.Message
                });
            }
        }
    }

    public class CreateUserBasicRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        public string? Username { get; set; }

        [EmailAddress]
        public string? Email { get; set; }
    }
}
