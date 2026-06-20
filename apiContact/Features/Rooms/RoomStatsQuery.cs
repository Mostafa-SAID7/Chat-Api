using apiContact.Data.Repositories;
using apiContact.Models.Dtos;
using MediatR;

namespace apiContact.Features.Rooms
{
    /// <summary>
    /// Returns a lightweight statistics snapshot for a room:
    /// member count, total message count, pinned message count, and last activity.
    /// Returns null when the room does not exist or is deleted.
    /// </summary>
    public record GetRoomStatsQuery(string RoomId) : IRequest<RoomStatsDto?>;

    public class GetRoomStatsHandler : IRequestHandler<GetRoomStatsQuery, RoomStatsDto?>
    {
        private readonly IUnitOfWork _uow;
        public GetRoomStatsHandler(IUnitOfWork uow) => _uow = uow;

        public async Task<RoomStatsDto?> Handle(GetRoomStatsQuery q, CancellationToken ct)
        {
            var room = await _uow.Rooms.GetByIdAsync(q.RoomId);
            if (room is null || room.IsDeleted) return null;
            return await _uow.Rooms.GetStatsAsync(q.RoomId);
        }
    }
}
