using apiContact.Models.Entities;
using MongoDB.Driver;

namespace apiContact.Data.Indexes
{
    public static class UserIndexes
    {
        public static async Task EnsureAsync(IMongoCollection<ChatUser> col)
        {
            var models = new List<CreateIndexModel<ChatUser>>
            {
                // Unique username lookup
                new(
                    Builders<ChatUser>.IndexKeys.Ascending(u => u.Username),
                    new CreateIndexOptions { Unique = true, Name = "idx_users_username" }
                ),
                // Unique email lookup
                new(
                    Builders<ChatUser>.IndexKeys.Ascending(u => u.Email),
                    new CreateIndexOptions { Unique = true, Sparse = true, Name = "idx_users_email" }
                ),
                // Online presence queries
                new(
                    Builders<ChatUser>.IndexKeys.Ascending(u => u.IsOnline),
                    new CreateIndexOptions { Name = "idx_users_isOnline" }
                ),
                // Role-based filtering
                new(
                    Builders<ChatUser>.IndexKeys.Ascending(u => u.Role),
                    new CreateIndexOptions { Name = "idx_users_role" }
                )
            };

            await col.Indexes.CreateManyAsync(models);
        }
    }
}
