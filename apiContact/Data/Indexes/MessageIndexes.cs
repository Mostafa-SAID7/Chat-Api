using apiContact.Models.Entities;
using MongoDB.Driver;

namespace apiContact.Data.Indexes
{
    public static class MessageIndexes
    {
        public static async Task EnsureAsync(IMongoCollection<Message> col)
        {
            var models = new List<CreateIndexModel<Message>>
            {
                // Primary history query: all messages in a room, newest first
                new(
                    Builders<Message>.IndexKeys
                        .Ascending(m => m.RoomId)
                        .Descending(m => m.Timestamp),
                    new CreateIndexOptions { Name = "idx_messages_roomId_timestamp" }
                ),
                // Sender history lookup
                new(
                    Builders<Message>.IndexKeys.Ascending(m => m.SenderId),
                    new CreateIndexOptions { Name = "idx_messages_senderId" }
                ),
                // Unread count: roomId + isDeleted + readBy
                new(
                    Builders<Message>.IndexKeys
                        .Ascending(m => m.RoomId)
                        .Ascending(m => m.IsDeleted),
                    new CreateIndexOptions { Name = "idx_messages_roomId_isDeleted" }
                ),
                // TTL — optional: auto-expire messages after N days
                // Uncomment and set seconds to activate:
                // new(
                //     Builders<Message>.IndexKeys.Ascending(m => m.Timestamp),
                //     new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(90), Name = "idx_messages_ttl" }
                // ),
                // Message type filtering (images, files, etc.)
                new(
                    Builders<Message>.IndexKeys.Ascending(m => m.Type),
                    new CreateIndexOptions { Name = "idx_messages_type" }
                )
            };

            await col.Indexes.CreateManyAsync(models);
        }
    }
}
