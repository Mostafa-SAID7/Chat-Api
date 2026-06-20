using apiContact.Services;
using Microsoft.AspNetCore.SignalR;

namespace apiContact.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IUserService _users;

        public ChatHub(IUserService users) => _users = users;

        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _users.SetOnlineAsync(userId, true);
                await Clients.Others.SendAsync("UserOnline", new { userId, connectedAt = DateTime.UtcNow });
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _users.SetOnlineAsync(userId, false);
                await Clients.Others.SendAsync("UserOffline", new { userId, disconnectedAt = DateTime.UtcNow });
            }
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>Join a chat room group to receive its messages</summary>
        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserJoinedRoom", new
            {
                roomId,
                connectionId = Context.ConnectionId,
                joinedAt = DateTime.UtcNow
            });
        }

        /// <summary>Leave a chat room group</summary>
        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserLeftRoom", new
            {
                roomId,
                connectionId = Context.ConnectionId,
                leftAt = DateTime.UtcNow
            });
        }

        /// <summary>Broadcast typing indicator to room members</summary>
        public async Task Typing(string roomId, string userId, string displayName)
        {
            await Clients.OthersInGroup(roomId).SendAsync("UserTyping", new { roomId, userId, displayName });
        }

        /// <summary>Stop typing indicator</summary>
        public async Task StopTyping(string roomId, string userId)
        {
            await Clients.OthersInGroup(roomId).SendAsync("UserStoppedTyping", new { roomId, userId });
        }

        /// <summary>Send a ping to test connection</summary>
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", new { timestamp = DateTime.UtcNow });
        }
    }
}
