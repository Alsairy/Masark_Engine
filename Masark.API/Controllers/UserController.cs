using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Masark.Infrastructure.Identity;

namespace Masark.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<UserController> _logger;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<UserController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpGet("users")]
        [Authorize]
        public async Task<IActionResult> GetUsers([FromQuery] int limit = 50, [FromQuery] int offset = 0)
        {
            try
            {
                limit = Math.Min(limit, 200);

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                var users = await _userManager.Users
                    .Where(u => u.TenantId == currentUser.TenantId)
                    .Skip(offset)
                    .Take(limit)
                    .Select(u => new
                    {
                        id = u.Id,
                        username = u.UserName,
                        email = u.Email,
                        firstName = u.FirstName,
                        lastName = u.LastName,
                        fullName = u.GetFullName(),
                        isActive = u.IsActive,
                        tenantId = u.TenantId,
                        createdAt = u.CreatedAt.ToString("O"),
                        lastLoginAt = u.LastLoginAt.HasValue ? u.LastLoginAt.Value.ToString("O") : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    users = users,
                    total = await _userManager.Users.CountAsync(u => u.TenantId == currentUser.TenantId),
                    limit = limit,
                    offset = offset
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get users",
                    message = "An internal server error occurred"
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

                var existingUser = await _userManager.FindByNameAsync(request.Username) ?? 
                                  await _userManager.FindByEmailAsync(request.Email);

                if (existingUser != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "User with this username or email already exists"
                    });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                var tenantId = currentUser?.TenantId ?? 1;

                var user = new ApplicationUser
                {
                    UserName = request.Username,
                    Email = request.Email,
                    TenantId = tenantId,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, "TempPassword123!");
                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "User creation failed",
                        details = result.Errors.Select(e => e.Description)
                    });
                }

                await _userManager.AddToRoleAsync(user, "USER");

                return StatusCode(201, new
                {
                    success = true,
                    message = "User created successfully",
                    user = new
                    {
                        id = user.Id,
                        username = user.UserName,
                        email = user.Email,
                        isActive = user.IsActive,
                        tenantId = user.TenantId,
                        createdAt = user.CreatedAt.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to create user",
                    message = "An internal server error occurred"
                });
            }
        }

        [HttpGet("users/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetUser(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid user ID"
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "User not found"
                    });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || user.TenantId != currentUser.TenantId)
                {
                    return Forbid();
                }

                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        id = user.Id,
                        username = user.UserName,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        fullName = user.GetFullName(),
                        isActive = user.IsActive,
                        tenantId = user.TenantId,
                        roles = roles,
                        createdAt = user.CreatedAt.ToString("O"),
                        lastLoginAt = user.LastLoginAt.HasValue ? user.LastLoginAt.Value.ToString("O") : null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get user",
                    message = "An internal server error occurred"
                });
            }
        }

        [HttpPut("users/{userId}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
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

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "User not found"
                    });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || user.TenantId != currentUser.TenantId)
                {
                    return Forbid();
                }

                if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.UserName)
                {
                    var existingUser = await _userManager.FindByNameAsync(request.Username);
                    if (existingUser != null && existingUser.Id != userId)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            error = "Username already exists"
                        });
                    }
                    user.UserName = request.Username;
                }

                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(request.Email);
                    if (existingUser != null && existingUser.Id != userId)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            error = "Email already exists"
                        });
                    }
                    user.Email = request.Email;
                }

                if (!string.IsNullOrWhiteSpace(request.FirstName))
                {
                    user.FirstName = request.FirstName;
                }

                if (!string.IsNullOrWhiteSpace(request.LastName))
                {
                    user.LastName = request.LastName;
                }

                if (request.IsActive.HasValue)
                {
                    user.IsActive = request.IsActive.Value;
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "User update failed",
                        details = result.Errors.Select(e => e.Description)
                    });
                }

                if (!string.IsNullOrWhiteSpace(request.Role))
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var newRole = request.Role.ToUpper();
                    
                    if (new[] { "USER", "ADMIN" }.Contains(newRole) && !currentRoles.Contains(newRole))
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        await _userManager.AddToRoleAsync(user, newRole);
                    }
                }

                result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "User update failed",
                        details = result.Errors.Select(e => e.Description)
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "User updated successfully",
                    user = new
                    {
                        id = user.Id,
                        username = user.UserName,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        fullName = user.GetFullName(),
                        isActive = user.IsActive,
                        tenantId = user.TenantId,
                        updatedAt = DateTime.UtcNow.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to update user",
                    message = "An internal server error occurred"
                });
            }
        }

        [HttpDelete("users/{userId}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid user ID"
                    });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                if (userId == currentUser.Id)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Cannot delete your own account"
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "User not found"
                    });
                }

                if (user.TenantId != currentUser.TenantId)
                {
                    return Forbid();
                }

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "User deletion failed",
                        details = result.Errors.Select(e => e.Description)
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "User deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to delete user",
                    message = "An internal server error occurred"
                });
            }
        }

        [HttpPost("users/{userId}/roles")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(request.Role))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "User ID and role are required"
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "User not found"
                    });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || user.TenantId != currentUser.TenantId)
                {
                    return Forbid();
                }

                var role = request.Role.ToUpper();
                if (!new[] { "USER", "ADMIN" }.Contains(role))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid role. Must be USER or ADMIN"
                    });
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Contains(role))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = $"User already has {role} role"
                    });
                }

                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                var result = await _userManager.AddToRoleAsync(user, role);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Role assignment failed",
                        details = result.Errors.Select(e => e.Description)
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = $"Role {role} assigned successfully",
                    user = new
                    {
                        id = user.Id,
                        username = user.UserName,
                        email = user.Email,
                        role = role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role to user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to assign role",
                    message = "An internal server error occurred"
                });
            }
        }

        [HttpDelete("users/{userId}/roles/{role}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> RemoveRole(string userId, string role)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "User ID and role are required"
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "User not found"
                    });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || user.TenantId != currentUser.TenantId)
                {
                    return Forbid();
                }

                var roleUpper = role.ToUpper();
                var currentRoles = await _userManager.GetRolesAsync(user);
                
                if (!currentRoles.Contains(roleUpper))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = $"User does not have {roleUpper} role"
                    });
                }

                var result = await _userManager.RemoveFromRoleAsync(user, roleUpper);
                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Role removal failed",
                        details = result.Errors.Select(e => e.Description)
                    });
                }

                if (!currentRoles.Any(r => r != roleUpper))
                {
                    await _userManager.AddToRoleAsync(user, "USER");
                }

                return Ok(new
                {
                    success = true,
                    message = $"Role {roleUpper} removed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role from user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to remove role",
                    message = "An internal server error occurred"
                });
            }
        }

        [HttpGet("users/{userId}/roles")]
        [Authorize]
        public async Task<IActionResult> GetUserRoles(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "User ID is required"
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = "User not found"
                    });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || user.TenantId != currentUser.TenantId)
                {
                    return Forbid();
                }

                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        id = user.Id,
                        username = user.UserName,
                        email = user.Email,
                        roles = roles
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get user roles",
                    message = "An internal server error occurred"
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

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Role { get; set; }

        public bool? IsActive { get; set; }
    }

    public class AssignRoleRequest
    {
        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
