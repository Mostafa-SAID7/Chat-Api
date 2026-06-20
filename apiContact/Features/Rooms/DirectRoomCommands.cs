using apiContact.Data.Repositories;
using apiContact.Models.Entities;
using apiContact.Models.Enums;
using MediatR;

namespace apiContact.Features.Rooms
{
    /// <summary>
    /// Create or retrieve an existing Direct-Message room between two users.
    /// Idempotent: if a Direct room already exists for the pair, the existing room
    /// is returned rather than creating a duplicate.
    /// </summary>
    public record CreateDirectRoomCommand(string RequesterId, string TargetUserId)
        : IRequest<ChatRoom>;

    public class CreateDirectRoomHandler : IRequestHandler<CreateDirectRoomCommand, ChatRoom>
    {
        private readonly IUnitOfWork _uow;
        public CreateDirectRoomHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<ChatRoom> Handle(CreateDirectRoomCommand cmd, CancellationToken ct)
        {
            var existing = await _uow.Rooms.GetDirectRoomAsync(cmd.RequesterId, cmd.TargetUserId);
            if (existing is not null) return existing;

            int trim = 6;
            var r = cmd.RequesterId.Length >= trim ? cmd.RequesterId[..trim] : cmd.RequesterId;
            var t = cmd.TargetUserId.Length >= trim ? cmd.TargetUserId[..trim] : cmd.TargetUserId;

            var room = new ChatRoom
            {
                Id        = Guid.NewGuid().ToString(),
                Name      = $"dm-{r}-{t}",
                Slug      = $"dm-{Guid.NewGuid():N}",
                Type      = RoomType.Direct,
                IsPrivate = true,
                CreatedBy = cmd.RequesterId,
                MemberIds = new List<string> { cmd.RequesterId, cmd.TargetUserId },
                CreatedAt = DateTime.UtcNow,
            };
            return await _uow.Rooms.AddAsync(room);
        }
    }
}
