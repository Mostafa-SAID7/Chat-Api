using apiContact.Data.Repositories;
using apiContact.Models.Entities;
using MediatR;

namespace apiContact.Features.Rooms
{
    /// <summary>
    /// Generate (or regenerate) a URL-safe invite code for a room.
    /// The code is stored on the room document and expires after ExpiryHours.
    /// Returns null if the room does not exist.
    /// </summary>
    public record GenerateRoomInviteCommand(string RoomId, int ExpiryHours = 24) : IRequest<string?>;

    public class GenerateRoomInviteHandler : IRequestHandler<GenerateRoomInviteCommand, string?>
    {
        private readonly IUnitOfWork _uow;
        public GenerateRoomInviteHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<string?> Handle(GenerateRoomInviteCommand cmd, CancellationToken ct)
        {
            var room = await _uow.Rooms.GetByIdAsync(cmd.RoomId);
            if (room is null || room.IsDeleted) return null;

            // 12-char URL-safe base64 invite code
            var raw  = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var code = raw.Replace("+", "-").Replace("/", "_").Replace("=", "")[..12];
            await _uow.Rooms.SetInviteCodeAsync(cmd.RoomId, code,
                DateTime.UtcNow.AddHours(cmd.ExpiryHours));
            return code;
        }
    }

    /// <summary>
    /// Join a room using a valid, non-expired invite code.
    /// Returns null when the code is invalid, expired, or the room is at capacity.
    /// Idempotent: if the user is already a member the room is returned unchanged.
    /// </summary>
    public record JoinRoomByInviteCommand(string Code, string UserId) : IRequest<ChatRoom?>;

    public class JoinRoomByInviteHandler : IRequestHandler<JoinRoomByInviteCommand, ChatRoom?>
    {
        private readonly IUnitOfWork _uow;
        public JoinRoomByInviteHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ChatRoom?> Handle(JoinRoomByInviteCommand cmd, CancellationToken ct)
        {
            var room = await _uow.Rooms.GetByInviteCodeAsync(cmd.Code);
            if (room is null) return null;

            if (room.MaxMembers.HasValue && room.MemberIds.Count >= room.MaxMembers.Value)
                return null;    // capacity reached

            if (!room.MemberIds.Contains(cmd.UserId))
                await _uow.Rooms.AddMemberAsync(room.Id, cmd.UserId);

            return await _uow.Rooms.GetByIdAsync(room.Id);
        }
    }
}
