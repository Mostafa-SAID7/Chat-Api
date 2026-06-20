using System.Security.Claims;
using apiContact.Features.Users;
using apiContact.Mappings;
using apiContact.Models.Dtos;
using apiContact.Models.Enums;
using MediatR;
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
        private readonly IMediator _mediator;
        public UsersController(IMediator mediator) => _mediator = mediator;

        private string CallerRole =>
            User.FindFirstValue(ClaimTypes.Role) ?? "user";
        private string CallerId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";

        // ── Listing / search ──────────────────────────────────────

        /// <summary>List all users</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list     = await _mediator.Send(new GetAllUsersQuery());
            var profiles = list.Select(UserMapper.ToProfile);
            return Ok(ApiResponse<object>.Ok(profiles, total: list.Count));
        }

        /// <summary>Search users with pagination — supports q, role, onlineOnly filters</summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] UserSearchQuery q)
        {
            var result = await _mediator.Send(new SearchUsersQuery(q));
            return Ok(PagedApiResponse.From(result));
        }

        /// <summary>Get all currently online users</summary>
        [HttpGet("online")]
        public async Task<IActionResult> GetOnline()
        {
            var list     = await _mediator.Send(new GetOnlineUsersQuery());
            var profiles = list.Select(UserMapper.ToPublicProfile);
            return Ok(ApiResponse<object>.Ok(profiles, total: list.Count));
        }

        // ── Single user ───────────────────────────────────────────

        /// <summary>Get user by ID</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _mediator.Send(new GetUserByIdQuery(id));
            if (user is null) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<object>.Ok(UserMapper.ToProfile(user)));
        }

        // ── Mutations ─────────────────────────────────────────────

        /// <summary>Update profile fields (owner or admin)</summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
        {
            if (CallerId != id && !CallerRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return Forbid();
            var user = await _mediator.Send(new UpdateUserCommand(id, dto));
            if (user is null) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<object>.Ok(UserMapper.ToProfile(user), "Profile updated"));
        }

        /// <summary>Set full presence status (Online, Away, Busy, Offline)</summary>
        [HttpPost("{id}/status")]
        public async Task<IActionResult> SetStatus(string id, [FromBody] SetStatusDto dto)
        {
            if (CallerId != id && !CallerRole.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return Forbid();
            var ok = await _mediator.Send(new SetUserStatusCommand(id, dto.Status));
            if (!ok) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<object>.Ok(new { id, status = dto.Status.ToString() }, "Status updated"));
        }

        /// <summary>Delete a user (admin only)</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var ok = await _mediator.Send(new DeleteUserCommand(id));
            if (!ok) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<object>.Ok(new { id }, "User deleted"));
        }

        // ── Blocking ──────────────────────────────────────────────

        /// <summary>Block another user — their messages will be hidden and DMs rejected</summary>
        [HttpPost("{id}/block")]
        public async Task<IActionResult> BlockUser(string id)
        {
            if (id == CallerId) return BadRequest(ApiResponse<object>.Fail("Cannot block yourself"));
            var target = await _mediator.Send(new GetUserByIdQuery(id));
            if (target is null) return NotFound(ApiResponse<object>.Fail("User not found"));
            await _mediator.Send(new BlockUserCommand(CallerId, id));
            return Ok(ApiResponse<object>.Ok(new { blockedId = id }, "User blocked"));
        }

        /// <summary>Unblock a previously blocked user</summary>
        [HttpDelete("{id}/block")]
        public async Task<IActionResult> UnblockUser(string id)
        {
            await _mediator.Send(new UnblockUserCommand(CallerId, id));
            return Ok(ApiResponse<object>.Ok(new { unblockedId = id }, "User unblocked"));
        }

        /// <summary>Get the list of users blocked by the current user</summary>
        [HttpGet("blocked")]
        public async Task<IActionResult> GetBlocked()
        {
            var list     = await _mediator.Send(new GetBlockedUsersQuery(CallerId));
            var profiles = list.Select(UserMapper.ToPublicProfile);
            return Ok(ApiResponse<object>.Ok(profiles, total: list.Count));
        }
    }
}
