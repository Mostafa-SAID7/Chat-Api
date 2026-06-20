using System.Security.Claims;
using apiContact.Hubs;
using apiContact.Models.Dtos;
using apiContact.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace apiContact.Controllers
{
    [ApiController]
    [Route("api/messages")]
    [Produces("application/json")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messages;
        private readonly IRoomService    _rooms;
        private readonly IUserService    _users;
        private readonly IHubContext<ChatHub> _hub;

        public MessagesController(
            IMessageService messages,
            IRoomService rooms,
            IUserService users,
            IHubContext<ChatHub> hub)
        {
            _messages = messages;
            _rooms    = rooms;
            _users    = users;
            _hub      = hub;
        }

        /// <summary>Get paginated message history for a room</summary>
        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetByRoom(
            string roomId,
            [FromQuery] int limit = 50,
            [FromQuery] int skip  = 0)
        {
            var list = await _messages.GetByRoomAsync(roomId, limit, skip);
            return Ok(ApiResponse<object>.Ok(list, total: list.Count));
        }

        /// <summary>Get a single message by ID</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var msg = await _messages.GetByIdAsync(id);
            if (msg == null) return NotFound(ApiResponse<object>.Fail("Message not found"));
            return Ok(ApiResponse<object>.Ok(msg));
        }

        /// <summary>Send a message — broadcasts via WebSocket to all room subscribers</summary>
        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(ApiResponse<object>.Fail("Message content is required"));

            // Resolve sender from JWT; override any spoofed senderId
            var callerId     = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User.FindFirstValue("sub");
            var callerName   = User.FindFirstValue("displayName")
                            ?? User.FindFirstValue(ClaimTypes.Name)
                            ?? dto.SenderId;

            dto.SenderId = callerId!;

            var msg = await _messages.SendAsync(dto, callerName!);

            var preview = dto.Content.Length > 60
                ? dto.Content[..60] + "…"
                : dto.Content;
            await _rooms.UpdateLastMessageAsync(dto.RoomId, preview);

            await _hub.Clients.Group(dto.RoomId).SendAsync("ReceiveMessage", new
            {
                msg.Id, msg.RoomId, msg.SenderId, msg.SenderName,
                msg.Content, Type = msg.Type.ToString(), msg.Timestamp
            });

            return CreatedAtAction(nameof(GetById), new { id = msg.Id },
                ApiResponse<object>.Ok(msg, "Message sent"));
        }

        /// <summary>Edit a message (sender only)</summary>
        [HttpPatch("{id}")]
        public async Task<IActionResult> Edit(string id, [FromBody] EditMessageDto dto)
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");

            var msg = await _messages.GetByIdAsync(id);
            if (msg == null) return NotFound(ApiResponse<object>.Fail("Message not found"));
            if (msg.SenderId != callerId)
                return Forbid();

            var updated = await _messages.EditAsync(id, callerId!, dto.Content);

            await _hub.Clients.Group(msg.RoomId)
                .SendAsync("MessageEdited", new { id, content = dto.Content });

            return Ok(ApiResponse<object>.Ok(updated, "Message edited"));
        }

        /// <summary>Delete a message (sender or admin)</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var callerId   = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub");
            var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? "user";

            var msg = await _messages.GetByIdAsync(id);
            if (msg == null) return NotFound(ApiResponse<object>.Fail("Message not found"));

            if (msg.SenderId != callerId && callerRole != "admin")
                return Forbid();

            await _messages.DeleteAsync(id, msg.SenderId);

            await _hub.Clients.Group(msg.RoomId)
                .SendAsync("MessageDeleted", new { id, roomId = msg.RoomId });

            return Ok(ApiResponse<object>.Ok(new { id }, "Message deleted"));
        }

        /// <summary>Mark a message as read by the current user</summary>
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(string id)
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");
            await _messages.MarkReadAsync(id, callerId!);
            return Ok(ApiResponse<object>.Ok(new { id, userId = callerId }, "Marked as read"));
        }

        /// <summary>Get unread message count in a room for the current user</summary>
        [HttpGet("room/{roomId}/unread")]
        public async Task<IActionResult> UnreadCount(string roomId)
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub");
            var count = await _messages.GetUnreadCountAsync(roomId, callerId!);
            return Ok(ApiResponse<object>.Ok(new { roomId, userId = callerId, unreadCount = count }));
        }
    }
}
