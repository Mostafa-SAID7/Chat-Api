# 💾 Database Documentation

## Overview

Chat API supports multiple database backends with MongoDB as the primary choice and an in-memory fallback for development.

---

## Database Options

### 1. MongoDB (Recommended)

MongoDB is a NoSQL document database ideal for flexible schemas and horizontal scaling.

**Advantages:**
- Flexible document structure
- Horizontal scalability
- Built-in replication
- Atlas cloud hosting available
- Great for hierarchical data

**Connection String Format:**
```
mongodb://[username:password@]host[:port]/[database]?options
```

**Local Connection:**
```
mongodb://localhost:27017/chatdb
```

**MongoDB Atlas (Cloud):**
```
mongodb+srv://username:password@cluster0.xxxxx.mongodb.net/chatdb?retryWrites=true&w=majority
```

### 2. In-Memory Database (Development)

Falls back automatically when MongoDB connection string is not configured.

**Advantages:**
- No external dependencies
- Fast for testing
- Perfect for prototyping
- Data lost on restart

**Note:** Only for development use. Not for production.

---

## Collections Schema

### Users Collection

```json
{
  "_id": ObjectId("507f1f77bcf86cd799439011"),
  "email": "john@example.com",
  "passwordHash": "$2b$12$...",
  "firstName": "John",
  "lastName": "Doe",
  "avatar": "https://example.com/avatar.jpg",
  "phone": "+1234567890",
  "status": "active",
  "isOnline": true,
  "lastSeenAt": ISODate("2026-06-20T10:30:00Z"),
  "createdAt": ISODate("2026-06-15T08:00:00Z"),
  "updatedAt": ISODate("2026-06-20T10:30:00Z"),
  "isDeleted": false,
  "deletedAt": null
}
```

**Indexes:**
```javascript
db.users.createIndex({ email: 1 }, { unique: true })
db.users.createIndex({ createdAt: -1 })
db.users.createIndex({ isDeleted: 1 })
```

### Contacts Collection

```json
{
  "_id": ObjectId("507f1f77bcf86cd799439012"),
  "userId": ObjectId("507f1f77bcf86cd799439011"),
  "contactUserId": ObjectId("507f1f77bcf86cd799439013"),
  "nickname": "Johnny",
  "notes": "College friend",
  "isBlocked": false,
  "blockedAt": null,
  "isMuted": false,
  "createdAt": ISODate("2026-06-15T08:00:00Z"),
  "updatedAt": ISODate("2026-06-20T10:30:00Z"),
  "isDeleted": false
}
```

**Indexes:**
```javascript
db.contacts.createIndex({ userId: 1, contactUserId: 1 }, { unique: true })
db.contacts.createIndex({ userId: 1, isDeleted: 1 })
db.contacts.createIndex({ createdAt: -1 })
```

### Messages Collection

```json
{
  "_id": ObjectId("507f1f77bcf86cd799439014"),
  "senderId": ObjectId("507f1f77bcf86cd799439011"),
  "receiverId": ObjectId("507f1f77bcf86cd799439013"),
  "conversationId": ObjectId("507f1f77bcf86cd799439015"),
  "text": "Hello! How are you?",
  "attachments": [
    {
      "id": "file123",
      "fileName": "document.pdf",
      "fileSize": 102400,
      "fileType": "application/pdf",
      "url": "https://example.com/uploads/file123.pdf",
      "uploadedAt": ISODate("2026-06-20T10:30:00Z")
    }
  ],
  "isRead": true,
  "readAt": ISODate("2026-06-20T10:35:00Z"),
  "isEdited": false,
  "editedAt": null,
  "editHistory": [],
  "reactions": [
    {
      "userId": ObjectId("507f1f77bcf86cd799439013"),
      "emoji": "👍",
      "createdAt": ISODate("2026-06-20T10:32:00Z")
    }
  ],
  "createdAt": ISODate("2026-06-20T10:30:00Z"),
  "updatedAt": ISODate("2026-06-20T10:35:00Z"),
  "isDeleted": false,
  "deletedAt": null
}
```

**Indexes:**
```javascript
db.messages.createIndex({ senderId: 1, receiverId: 1, createdAt: -1 })
db.messages.createIndex({ conversationId: 1, createdAt: -1 })
db.messages.createIndex({ receiverId: 1, isRead: 1 })
db.messages.createIndex({ createdAt: 1 }, { expireAfterSeconds: 7776000 }) // Optional: TTL
```

### Conversations Collection

```json
{
  "_id": ObjectId("507f1f77bcf86cd799439015"),
  "participants": [
    ObjectId("507f1f77bcf86cd799439011"),
    ObjectId("507f1f77bcf86cd799439013")
  ],
  "lastMessage": {
    "id": ObjectId("507f1f77bcf86cd799439014"),
    "text": "Hello! How are you?",
    "senderId": ObjectId("507f1f77bcf86cd799439011"),
    "createdAt": ISODate("2026-06-20T10:30:00Z")
  },
  "unreadCount": 0,
  "isPinned": false,
  "isMuted": false,
  "mutedUntil": null,
  "createdAt": ISODate("2026-06-15T08:00:00Z"),
  "updatedAt": ISODate("2026-06-20T10:30:00Z")
}
```

**Indexes:**
```javascript
db.conversations.createIndex({ participants: 1 })
db.conversations.createIndex({ updatedAt: -1 })
```

### Attachments Collection

```json
{
  "_id": ObjectId("507f1f77bcf86cd799439016"),
  "messageId": ObjectId("507f1f77bcf86cd799439014"),
  "uploadedBy": ObjectId("507f1f77bcf86cd799439011"),
  "fileName": "document.pdf",
  "originalFileName": "my-document.pdf",
  "fileSize": 102400,
  "fileType": "application/pdf",
  "mimeType": "application/pdf",
  "url": "https://example.com/uploads/507f1f77bcf86cd799439016.pdf",
  "thumbnail": "https://example.com/uploads/507f1f77bcf86cd799439016_thumb.jpg",
  "uploadedAt": ISODate("2026-06-20T10:30:00Z"),
  "isDeleted": false
}
```

---

## Relationships

### One-to-Many (User → Messages)

```csharp
// User sends many messages
// Query: Get all messages from user
var userMessages = await db.messages
    .find({ senderId: userId })
    .toArray();
```

### Many-to-Many (User ↔ Contact)

```csharp
// Users have many contacts
// Query: Get user's contacts
var contacts = await db.contacts
    .aggregate([
        { $match: { userId: userId, isDeleted: false } },
        { $lookup: {
            from: "users",
            localField: "contactUserId",
            foreignField: "_id",
            as: "contactUser"
        }}
    ])
    .toArray();
```

### Embedded Documents (Message → Attachments)

```json
{
  "_id": ObjectId("..."),
  "text": "Check this file",
  "attachments": [
    {
      "id": "file1",
      "fileName": "report.pdf",
      "url": "https://..."
    }
  ]
}
```

---

## Data Access Patterns

### Repository Pattern

Generic repository for all entities:

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(string id);
    Task<int> CountAsync();
}
```

### Custom Repository Example

```csharp
public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetConversationAsync(
        string userId1, 
        string userId2, 
        int page = 1, 
        int pageSize = 20);
    
    Task<IEnumerable<Message>> GetUnreadMessagesAsync(string userId);
    
    Task<Message> GetLastMessageAsync(string conversationId);
}
```

### Unit of Work Pattern

```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IContactRepository Contacts { get; }
    IMessageRepository Messages { get; }
    IConversationRepository Conversations { get; }
    IAttachmentRepository Attachments { get; }
    
    Task SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

---

## Query Examples

### Aggregation Pipeline

**Get conversation with last 10 messages:**

```javascript
db.messages.aggregate([
  {
    $match: {
      $or: [
        { senderId: userId1, receiverId: userId2 },
        { senderId: userId2, receiverId: userId1 }
      ]
    }
  },
  {
    $sort: { createdAt: -1 }
  },
  {
    $limit: 10
  },
  {
    $lookup: {
      from: "users",
      localField: "senderId",
      foreignField: "_id",
      as: "sender"
    }
  },
  {
    $unwind: "$sender"
  },
  {
    $sort: { createdAt: 1 }
  }
])
```

### Text Search

**Search messages by content:**

```javascript
// Create text index
db.messages.createIndex({ text: "text" })

// Search
db.messages.find(
  { $text: { $search: "hello world" } },
  { score: { $meta: "textScore" } }
).sort({ score: { $meta: "textScore" } })
```

### Pagination

```csharp
public async Task<PagedResult<Message>> GetMessagesAsync(
    string conversationId, 
    int page = 1, 
    int pageSize = 20)
{
    var skip = (page - 1) * pageSize;
    
    var filter = Builders<Message>.Filter.Eq(m => m.ConversationId, conversationId);
    
    var total = await Collection.CountDocumentsAsync(filter);
    
    var messages = await Collection
        .Find(filter)
        .Sort(Builders<Message>.Sort.Descending(m => m.CreatedAt))
        .Skip(skip)
        .Limit(pageSize)
        .ToListAsync();
    
    return new PagedResult<Message>
    {
        Data = messages,
        Total = (int)total,
        Page = page,
        PageSize = pageSize
    };
}
```

---

## Transactions

**Multi-document transaction example:**

```csharp
using (var session = await mongoClient.StartSessionAsync())
{
    session.StartTransaction();
    
    try
    {
        // Create message
        var message = new Message { /* ... */ };
        await messagesCollection.InsertOneAsync(session, message);
        
        // Update conversation last message
        var update = Builders<Conversation>.Update
            .Set(c => c.LastMessage, message)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);
        
        await conversationsCollection.UpdateOneAsync(
            session,
            Builders<Conversation>.Filter.Eq(c => c.Id, conversationId),
            update);
        
        await session.CommitTransactionAsync();
    }
    catch (Exception)
    {
        await session.AbortTransactionAsync();
        throw;
    }
}
```

---

## Performance Optimization

### Indexing Strategy

```javascript
// High-priority indexes
db.users.createIndex({ email: 1 })
db.messages.createIndex({ conversationId: 1, createdAt: -1 })
db.contacts.createIndex({ userId: 1, isDeleted: 1 })

// Optional indexes for specific queries
db.messages.createIndex({ senderId: 1, createdAt: -1 })
db.messages.createIndex({ receiverId: 1, isRead: 1 })
```

### Projection

**Return only needed fields:**

```csharp
var projection = Builders<Message>.Projection
    .Include(m => m.Text)
    .Include(m => m.SenderId)
    .Include(m => m.CreatedAt)
    .Exclude(m => m.Id);

var messages = await Collection
    .Find(filter)
    .Project<MessageDto>(projection)
    .ToListAsync();
```

### Aggregation Optimization

```javascript
// Move $match early in pipeline
db.messages.aggregate([
  { $match: { createdAt: { $gt: ISODate("2026-01-01") } } },  // Filter first
  { $lookup: { /* ... */ } },                                   // Then join
  { $group: { /* ... */ } }                                     // Finally group
])
```

---

## Backup & Recovery

### MongoDB Atlas Backup

```bash
# Enable automatic backups in Atlas Console
# Settings → Project Settings → Backup
```

### Manual Backup

```bash
# Export collection
mongoexport --db chatdb --collection users --out users.json

# Import collection
mongoimport --db chatdb --collection users --file users.json
```

### Data Retention

Messages older than 90 days (optional TTL):

```javascript
db.messages.createIndex(
    { createdAt: 1 },
    { expireAfterSeconds: 7776000 }  // 90 days
)
```

---

## Monitoring

### Query Performance

Enable profiling:

```javascript
db.setProfilingLevel(1, { slowms: 100 })

// View slow queries
db.system.profile.find({millis: { $gt: 100 }}).pretty()
```

### Connection Monitoring

```csharp
var settings = MongoClientSettings.FromUrl(
    new MongoUrl(connectionString));

settings.ServerMonitor = 
    new ServerMonitor(new ServerMonitorSettings());
```

---

## Migration Guide

### Adding a New Field

```javascript
// 1. Add with default value
db.users.updateMany({}, { $set: { newField: null } })

// 2. Update existing documents
db.users.updateMany(
    { newField: null },
    { $set: { newField: "default" } }
)
```

### Removing a Field

```javascript
db.users.updateMany({}, { $unset: { obsoleteField: "" } })
```

### Renaming a Field

```javascript
db.users.updateMany({}, { $rename: { oldField: "newField" } })
```

---

See also:
- [Architecture Guide](./ARCHITECTURE.md)
- [API Documentation](./API.md)
- [Setup Guide](./SETUP.md)
