using apiContact.Models.Entities;
using MongoDB.Driver;

namespace apiContact.Data.Indexes
{
    public static class RoomIndexes
    {
        public static async Task EnsureAsync(IMongoCollection<ChatRoom> col)
        {
            var models = new List<CreateIndexModel<ChatRoom>>
            {
                // Member lookup — "rooms for user X"
                new(
                    Builders<ChatRoom>.IndexKeys.Ascending(r => r.MemberIds),
                    new CreateIndexOptions { Name = "idx_rooms_memberIds" }
                ),
                // Sort by latest activity (default room list order)
                new(
                    Builders<ChatRoom>.IndexKeys.Descending(r => r.LastMessageAt),
                    new CreateIndexOptions { Name = "idx_rooms_lastMessageAt" }
                ),
                // Name search / deduplication
                new(
                    Builders<ChatRoom>.IndexKeys.Ascending(r => r.Name),
                    new CreateIndexOptions { Name = "idx_rooms_name" }
                ),
                // Room type filtering
                new(
                    Builders<ChatRoom>.IndexKeys.Ascending(r => r.Type),
                    new CreateIndexOptions { Name = "idx_rooms_type" }
                ),
                // Creator lookup
                new(
                    Builders<ChatRoom>.IndexKeys.Ascending(r => r.CreatedBy),
                    new CreateIndexOptions { Name = "idx_rooms_createdBy" }
                )
            };

            await col.Indexes.CreateManyAsync(models);
        }
    }
}
