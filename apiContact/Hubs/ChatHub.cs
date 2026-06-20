using System.Security.Claims;
using apiContact.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace apiContact.Hubs
{
    /// <summary>
    /// Real-time WebSocket hub.
    /// [Authorize] is required — unauthenticated clients are rejected at the transport layer.
    /// User identity is always read from the validated JWT claims (Context.User),
    /// never from query string parameters, preventing identity spoofing.
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IUserService           _users;
        private readonly ILogger<ChatHub>       _log;

        public ChatHub(IUserService users, ILogger<ChatHub> log)
        {
            _users = users;
            _log   = log;
        }

        private string? CallerUserId =>
            Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
         ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);

        private string? CallerUsername =>
            Context.User?.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
         ?? Context.User?.FindFirstValue(ClaimTypes.Name);

        public override async Task OnConnectedAsync()
        {
            var userId = CallerUserId;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _users.SetOnlineAsync(userId, true);
                _log.LogInformation("SignalR connected user={UserId} conn={ConnId}",
                    userId, Context.ConnectionId);
                await Clients.Others.SendAsync("UserOnline", new
                {
                    userId,
                    username    = CallerUsername,
                    connectedAt = DateTime.UtcNow
                });
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = CallerUserId;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _users.SetOnlineAsync(userId, false);
                _log.LogInformation("SignalR disconnected user={UserId} conn={ConnId} error={Error}",
                    userId, Context.ConnectionId, exception?.Message);
                await Clients.Others.SendAsync("UserOffline", new
                {
                    userId,
                    disconnectedAt = DateTime.UtcNow
                });
            }
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>Join a chat room group to receive its messages</summary>
        public async Task JoinRoom(string roomId)
        {
            var userId = CallerUserId ?? Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            _log.LogDebug("User {UserId} joined room {RoomId}", userId, roomId);
            await Clients.Group(roomId).SendAsync("UserJoinedRoom", new
            {
                roomId,
                userId,
                connectionId = Context.ConnectionId,
                joinedAt     = DateTime.UtcNow
            });
        }

        /// <summary>Leave a chat room group</summary>
        public async Task LeaveRoom(string roomId)
        {
            var userId = CallerUserId ?? Context.ConnectionId;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserLeftRoom", new
            {
                roomId,
                userId,
                connectionId = Context.ConnectionId,
                leftAt       = DateTime.UtcNow
            });
        }

        /// <summary>Broadcast typing indicator — identity verified from JWT, not client payload</summary>
        public async Task Typing(string roomId)
        {
            var userId      = CallerUserId      ?? Context.ConnectionId;
            var displayName = CallerUsername    ?? "Unknown";
            await Clients.OthersInGroup(roomId).SendAsync("UserTyping", new
            {
                roomId,
                userId,
                displayName
            });
        }

        /// <summary>Stop typing indicator</summary>
        public async Task StopTyping(string roomId)
        {
            var userId = CallerUserId ?? Context.ConnectionId;
            await Clients.OthersInGroup(roomId).SendAsync("UserStoppedTyping", new { roomId, userId });
        }

        /// <summary>Ping/pong health check</summary>
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", new { timestamp = DateTime.UtcNow });
        }
    }

    // Keep the constant accessible for token claims
    internal static class JwtRegisteredClaimNames
    {
        public const string Sub        = "sub";
        public const string UniqueName = "unique_name";
    }
}
