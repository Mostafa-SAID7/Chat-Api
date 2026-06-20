using System.Security.Claims;
using apiContact.Models.Dtos;
using apiContact.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace apiContact.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _users;

        public UsersController(IUserService users) => _users = users;

        /// <summary>List all users (public profiles — no password hash)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _users.GetAllAsync();
            var profiles = list.Select(u => new
            {
                u.Id, u.Username, u.DisplayName,
                u.Email, u.AvatarUrl, u.Role,
                u.IsOnline, u.LastSeen, u.CreatedAt
            });
            return Ok(ApiResponse<object>.Ok(profiles, total: list.Count));
        }

        /// <summary>Get user by ID</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _users.GetByIdAsync(id);
            if (user == null) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<object>.Ok(new
            {
                user.Id, user.Username, user.DisplayName,
                user.Email, user.AvatarUrl, user.Role,
                user.IsOnline, user.LastSeen, user.CreatedAt
            }));
        }

        /// <summary>Get user by username</summary>
        [HttpGet("by-username/{username}")]
        public async Task<IActionResult> GetByUsername(string username)
        {
            var user = await _users.GetByUsernameAsync(username);
            if (user == null) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<object>.Ok(new
            {
                user.Id, user.Username, user.DisplayName,
                user.Email, user.AvatarUrl, user.Role,
                user.IsOnline, user.LastSeen, user.CreatedAt
            }));
        }

        /// <summary>Update your own profile (display name, avatar)</summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");

            // Only the owner or an admin can update
            var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? "user";
            if (callerId != id && callerRole != "admin")
                return Forbid();

            var user = await _users.UpdateAsync(id, dto);
            if (user == null) return NotFound(ApiResponse<object>.Fail("User not found"));

            return Ok(ApiResponse<object>.Ok(new
            {
                user.Id, user.Username, user.DisplayName,
                user.AvatarUrl, user.IsOnline
            }, "Profile updated"));
        }

        /// <summary>Set your own online status</summary>
        [HttpPost("{id}/status")]
        public async Task<IActionResult> SetStatus(string id, [FromBody] UpdateUserDto dto)
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");

            var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? "user";
            if (callerId != id && callerRole != "admin")
                return Forbid();

            await _users.SetOnlineAsync(id, dto.IsOnline ?? false);
            return Ok(ApiResponse<object>.Ok(new { id, isOnline = dto.IsOnline }, "Status updated"));
        }

        /// <summary>Delete a user (admin only)</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var ok = await _users.DeleteAsync(id);
            if (!ok) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<object>.Ok(new { id }, "User deleted"));
        }
    }
}
