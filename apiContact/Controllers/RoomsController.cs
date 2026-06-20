using System.Security.Claims;
using apiContact.Models.Dtos;
using apiContact.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace apiContact.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    [Produces("application/json")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _rooms;

        public RoomsController(IRoomService rooms) => _rooms = rooms;

        /// <summary>Get all rooms</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _rooms.GetAllAsync();
            return Ok(ApiResponse<object>.Ok(list, total: list.Count));
        }

        /// <summary>Get rooms for the current user</summary>
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");
            var list = await _rooms.GetByUserAsync(userId!);
            return Ok(ApiResponse<object>.Ok(list, total: list.Count));
        }

        /// <summary>Get rooms for a specific user</summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(string userId)
        {
            var list = await _rooms.GetByUserAsync(userId);
            return Ok(ApiResponse<object>.Ok(list, total: list.Count));
        }

        /// <summary>Get room by ID</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var room = await _rooms.GetByIdAsync(id);
            if (room == null) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(room));
        }

        /// <summary>Create a new room</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(ApiResponse<object>.Fail("Room name is required"));

            // Automatically include the caller as a member and creator
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");

            dto.CreatedBy = callerId!;
            if (!dto.MemberIds.Contains(callerId!))
                dto.MemberIds.Add(callerId!);

            var room = await _rooms.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = room.Id },
                ApiResponse<object>.Ok(room, "Room created"));
        }

        /// <summary>Add a member to a room</summary>
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(string id, [FromBody] AddMemberDto dto)
        {
            var ok = await _rooms.AddMemberAsync(id, dto.UserId);
            if (!ok) return BadRequest(ApiResponse<object>.Fail("Could not add member — already a member or room not found"));
            return Ok(ApiResponse<object>.Ok(new { roomId = id, userId = dto.UserId }, "Member added"));
        }

        /// <summary>Remove a member from a room</summary>
        [HttpDelete("{id}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(string id, string userId)
        {
            var ok = await _rooms.RemoveMemberAsync(id, userId);
            if (!ok) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(new { roomId = id, userId }, "Member removed"));
        }

        /// <summary>Delete a room (admin only)</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var ok = await _rooms.DeleteAsync(id);
            if (!ok) return NotFound(ApiResponse<object>.Fail("Room not found"));
            return Ok(ApiResponse<object>.Ok(new { id }, "Room deleted"));
        }
    }
}
