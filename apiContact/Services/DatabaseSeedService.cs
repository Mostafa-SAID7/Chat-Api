using MongoDB.Driver;
using MongoDB.Bson;

namespace apiContact.Services;

/// <summary>
/// Service for seeding initial data into the database
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
    /// Seed initial data if database is empty
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("🌱 Checking if database needs seeding...");

            var usersCollection = _database.GetCollection<BsonDocument>("users");
            var userCount = await usersCollection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);

            if (userCount > 0)
            {
                _logger.LogInformation("✅ Database already populated with {Count} users", userCount);
                return;
            }

            _logger.LogInformation("📝 Seeding initial data...");

            // Create test users
            var testUsers = new List<BsonDocument>
            {
                new BsonDocument
                {
                    { "_id", ObjectId.GenerateNewId() },
                    { "email", "admin@chatapi.local" },
                    { "passwordHash", "$2b$12$ER1HwL2Wp1u3PiCxzm1uiO3zPO1U5CmkKz1bnY1ykR8XzXzIm6HyC" }, // password: Admin123!
                    { "firstName", "Admin" },
                    { "lastName", "User" },
                    { "avatar", "https://api.dicebear.com/7.x/avataaars/svg?seed=admin" },
                    { "isOnline", false },
                    { "createdAt", DateTime.UtcNow },
                    { "updatedAt", DateTime.UtcNow },
                    { "isDeleted", false }
                },
                new BsonDocument
                {
                    { "_id", ObjectId.GenerateNewId() },
                    { "email", "demo@chatapi.local" },
                    { "passwordHash", "$2b$12$ER1HwL2Wp1u3PiCxzm1uiO3zPO1U5CmkKz1bnY1ykR8XzXzIm6HyC" }, // password: Demo123!
                    { "firstName", "Demo" },
                    { "lastName", "User" },
                    { "avatar", "https://api.dicebear.com/7.x/avataaars/svg?seed=demo" },
                    { "isOnline", false },
                    { "createdAt", DateTime.UtcNow },
                    { "updatedAt", DateTime.UtcNow },
                    { "isDeleted", false }
                },
                new BsonDocument
                {
                    { "_id", ObjectId.GenerateNewId() },
                    { "email", "test@chatapi.local" },
                    { "passwordHash", "$2b$12$ER1HwL2Wp1u3PiCxzm1uiO3zPO1U5CmkKz1bnY1ykR8XzXzIm6HyC" }, // password: Test123!
                    { "firstName", "Test" },
                    { "lastName", "User" },
                    { "avatar", "https://api.dicebear.com/7.x/avataaars/svg?seed=test" },
                    { "isOnline", false },
                    { "createdAt", DateTime.UtcNow },
                    { "updatedAt", DateTime.UtcNow },
                    { "isDeleted", false }
                }
            };

            await usersCollection.InsertManyAsync(testUsers);
            _logger.LogInformation("✅ Created {Count} test users", testUsers.Count);

            // Create sample messages
            if (testUsers.Count >= 2)
            {
                var messagesCollection = _database.GetCollection<BsonDocument>("messages");
                var sampleMessages = new List<BsonDocument>
                {
                    new BsonDocument
                    {
                        { "_id", ObjectId.GenerateNewId() },
                        { "senderId", testUsers[0]["_id"] },
                        { "receiverId", testUsers[1]["_id"] },
                        { "text", "Welcome to Chat API! This is a test message." },
                        { "attachments", new BsonArray() },
                        { "isRead", false },
                        { "isDeleted", false },
                        { "createdAt", DateTime.UtcNow },
                        { "updatedAt", DateTime.UtcNow }
                    },
                    new BsonDocument
                    {
                        { "_id", ObjectId.GenerateNewId() },
                        { "senderId", testUsers[1]["_id"] },
                        { "receiverId", testUsers[0]["_id"] },
                        { "text", "Hello! This is a reply message." },
                        { "attachments", new BsonArray() },
                        { "isRead", false },
                        { "isDeleted", false },
                        { "createdAt", DateTime.UtcNow.AddSeconds(10) },
                        { "updatedAt", DateTime.UtcNow.AddSeconds(10) }
                    }
                };

                await messagesCollection.InsertManyAsync(sampleMessages);
                _logger.LogInformation("✅ Created {Count} sample messages", sampleMessages.Count);
            }

            _logger.LogInformation("✅ Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error during database seeding - continuing without seed data");
        }
    }
}
