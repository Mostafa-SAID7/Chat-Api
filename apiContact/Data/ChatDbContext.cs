using apiContact.Data.Indexes;
using apiContact.Data.Seeds;
using apiContact.Models.Entities;
using MongoDB.Driver;

namespace apiContact.Data
{
    public class ChatDbContext
    {
        private readonly IMongoDatabase? _database;
        private readonly bool _useInMemory;

        // In-memory fallback stores
        internal readonly Dictionary<string, ChatUser> Users    = new();
        internal readonly Dictionary<string, ChatRoom> Rooms    = new();
        internal readonly Dictionary<string, Message>  Messages = new();

        public ChatDbContext(IConfiguration config)
        {
            var connStr = config["MongoDB:ConnectionString"];
            var dbName  = config["MongoDB:DatabaseName"] ?? "ChatDb";

            if (!string.IsNullOrWhiteSpace(connStr))
            {
                try
                {
                    var client = new MongoClient(connStr);
                    _database    = client.GetDatabase(dbName);
                    _useInMemory = false;
                    _ = EnsureIndexesAsync();   // fire-and-forget on startup
                }
                catch
                {
                    _useInMemory = true;
                }
            }
            else
            {
                _useInMemory = true;
            }

            if (_useInMemory) ApplySeeds();
        }

        public bool IsInMemory => _useInMemory;

        public IMongoCollection<T>? GetCollection<T>(string name)
            => _database?.GetCollection<T>(name);

        // ── Seeds ────────────────────────────────────────────────────
        private void ApplySeeds()
        {
            var users    = UserSeed.Generate();
            var rooms    = RoomSeed.Generate(users);
            var messages = MessageSeed.Generate(users, rooms);

            foreach (var u in users)    Users[u.Id]    = u;
            foreach (var r in rooms)    Rooms[r.Id]    = r;
            foreach (var m in messages) Messages[m.Id] = m;

            // Backfill last-message preview on each room
            foreach (var room in Rooms.Values)
            {
                var last = Messages.Values
                    .Where(m => m.RoomId == room.Id && !m.IsDeleted)
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefault();

                if (last is null) continue;
                room.LastMessagePreview = last.Content;
                room.LastMessageAt      = last.Timestamp;
            }
        }

        // ── Indexes ──────────────────────────────────────────────────
        private async Task EnsureIndexesAsync()
        {
            if (_database is null) return;
            await Task.WhenAll(
                UserIndexes   .EnsureAsync(_database.GetCollection<ChatUser>("users")),
                RoomIndexes   .EnsureAsync(_database.GetCollection<ChatRoom>("rooms")),
                MessageIndexes.EnsureAsync(_database.GetCollection<Message> ("messages"))
            );
        }
    }
}
