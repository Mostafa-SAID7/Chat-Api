using MongoDB.Driver;
using System.Diagnostics;

namespace apiContact.Services;

/// <summary>
/// Service for handling MongoDB database migrations and initialization
/// </summary>
public interface IDatabaseMigrationService
{
    Task InitializeDatabaseAsync();
    Task VerifyConnectionAsync();
    Task CreateIndexesAsync();
}

public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IMongoClient _mongoClient;
    private readonly IMongoDatabase _database;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        IMongoClient mongoClient,
        IMongoDatabase database,
        ILogger<DatabaseMigrationService> logger)
    {
        _mongoClient = mongoClient ?? throw new ArgumentNullException(nameof(mongoClient));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Verify MongoDB connection is working
    /// </summary>
    public async Task VerifyConnectionAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Verifying MongoDB connection...");
            
            var stopwatch = Stopwatch.StartNew();
            
            // Ping the server to verify connection
            var adminDatabase = _mongoClient.GetDatabase("admin");
            var pingCommand = new MongoDB.Bson.BsonDocument("ping", 1);
            var pingResult = await adminDatabase.RunCommandAsync<MongoDB.Bson.BsonDocument>(
                pingCommand);
            
            stopwatch.Stop();
            
            _logger.LogInformation(
                "✅ MongoDB connection verified successfully (Response time: {ElapsedMs}ms)",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MongoDB connection verification failed");
            throw;
        }
    }

    /// <summary>
    /// Initialize database with required collections and indexes
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("🚀 Initializing database...");

            // Create indexes
            await CreateIndexesAsync();

            _logger.LogInformation("✅ Database initialization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Database initialization failed");
            throw;
        }
    }

    /// <summary>
    /// Create required database indexes for performance
    /// </summary>
    public async Task CreateIndexesAsync()
    {
        try
        {
            _logger.LogInformation("📊 Creating database indexes...");

            // Users collection indexes
            var usersCollection = _database.GetCollection<MongoDB.Bson.BsonDocument>("users");
            
            var userIndexes = new[]
            {
                new CreateIndexModel<MongoDB.Bson.BsonDocument>(
                    Builders<MongoDB.Bson.BsonDocument>.IndexKeys.Ascending("email"),
                    new CreateIndexOptions { Unique = true, Name = "idx_email_unique" }),
                new CreateIndexModel<MongoDB.Bson.BsonDocument>(
                    Builders<MongoDB.Bson.BsonDocument>.IndexKeys.Descending("createdAt"),
                    new CreateIndexOptions { Name = "idx_createdAt_desc" }),
                new CreateIndexModel<MongoDB.Bson.BsonDocument>(
                    Builders<MongoDB.Bson.BsonDocument>.IndexKeys.Ascending("isDeleted"),
                    new CreateIndexOptions { Name = "idx_isDeleted" })
            };

            await usersCollection.Indexes.CreateManyAsync(userIndexes);
            _logger.LogInformation("  ✅ Users indexes created");

            // Messages collection indexes
            var messagesCollection = _database.GetCollection<MongoDB.Bson.BsonDocument>("messages");
            
            var messageIndexes = new[]
            {
                new CreateIndexModel<MongoDB.Bson.BsonDocument>(
                    Builders<MongoDB.Bson.BsonDocument>.IndexKeys
                        .Ascending("senderId")
                        .Ascending("receiverId")
                        .Descending("createdAt"),
                    new CreateIndexOptions { Name = "idx_sender_receiver_date" }),
                new CreateIndexModel<MongoDB.Bson.BsonDocument>(
                    Builders<MongoDB.Bson.BsonDocument>.IndexKeys.Descending("createdAt"),
                    new CreateIndexOptions { Name = "idx_createdAt_desc" }),
                new CreateIndexModel<MongoDB.Bson.BsonDocument>(
                    Builders<MongoDB.Bson.BsonDocument>.IndexKeys
                        .Ascending("receiverId")
                        .Ascending("isRead"),
                    new CreateIndexOptions { Name = "idx_receiver_isRead" })
            };

            await messagesCollection.Indexes.CreateManyAsync(messageIndexes);
            _logger.LogInformation("  ✅ Messages indexes created");

            // Contacts collection indexes
            var contactsCollection = _database.GetCollection<MongoDB.Bson.BsonDocument>("contacts");
            
            var contactIndexes = new[]
            {
                new CreateIndexModel<MongoDB.Bson.BsonDocument>(
                    Builders<MongoDB.Bson.BsonDocument>.IndexKeys
                        .Ascending("userId")
                        .Ascending("contactUserId"),
                    new CreateIndexOptions 
                    { 
                        Unique = true,
                        Name = "idx_userId_contactUserId_unique" 
                    }),
                new CreateIndexModel<MongoDB.Bson.BsonDocument>(
                    Builders<MongoDB.Bson.BsonDocument>.IndexKeys
                        .Ascending("userId")
                        .Ascending("isDeleted"),
                    new CreateIndexOptions { Name = "idx_userId_isDeleted" })
            };

            await contactsCollection.Indexes.CreateManyAsync(contactIndexes);
            _logger.LogInformation("  ✅ Contacts indexes created");

            _logger.LogInformation("✅ All database indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to create database indexes");
            throw;
        }
    }
}
