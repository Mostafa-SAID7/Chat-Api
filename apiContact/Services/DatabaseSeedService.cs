using MongoDB.Driver;
using MongoDB.Bson;
using apiContact.Data.Seeds;
using apiContact.Models.Entities;

namespace apiContact.Services;

/// <summary>
/// Service for seeding initial data into the database using proper seed classes
/// </summary>
public interface IDatabaseSeedService
{
    Task SeedAsync();
}

public class DatabaseSeedService : IDatabaseSeedService
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<DatabaseSeedService> _logger;

    public DatabaseSeedService(
        IMongoDatabase database,
        ILogger<DatabaseSeedService> logger)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Seed initial data from seed classes if database is empty
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("🌱 Checking if database needs seeding...");

            var usersCollection = _database.GetCollection<ChatUser>("users");
            var userCount = await usersCollection.CountDocumentsAsync(Builders<ChatUser>.Filter.Empty);

            if (userCount > 0)
            {
                _logger.LogInformation("✅ Database already populated with {Count} users", userCount);
                return;
            }

            _logger.LogInformation("📝 Seeding initial data from seed classes...");

            // 1. Generate and seed Users
            var users = UserSeed.Generate();
            await usersCollection.InsertManyAsync(users);
            _logger.LogInformation("  ✅ Created {Count} users", users.Count);

            // 2. Generate and seed Rooms (depends on users)
            var roomsCollection = _database.GetCollection<ChatRoom>("rooms");
            var rooms = RoomSeed.Generate(users);
            await roomsCollection.InsertManyAsync(rooms);
            _logger.LogInformation("  ✅ Created {Count} rooms", rooms.Count);

            // 3. Generate and seed Messages (depends on users and rooms)
            var messagesCollection = _database.GetCollection<Message>("messages");
            var messages = MessageSeed.Generate(users, rooms);
            await messagesCollection.InsertManyAsync(messages);
            _logger.LogInformation("  ✅ Created {Count} messages", messages.Count);

            _logger.LogInformation("✅ Database seeding completed successfully");
            _logger.LogInformation("📊 Seeded Data Summary:");
            _logger.LogInformation("   • Users: alice (Admin), bob (User), carla (User)");
            _logger.LogInformation("   • Rooms: General, Engineering, Direct Message");
            _logger.LogInformation("   • Messages: Welcome message, engineering discussion, DM");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error during database seeding - continuing without seed data");
        }
    }
}
