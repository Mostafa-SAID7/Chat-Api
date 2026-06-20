# 🔧 Troubleshooting Guide

## Common Issues & Solutions

---

## Startup Issues

### Application Won't Start

#### Error: `JWT_KEY environment variable is required`

**Cause:** JWT secret key is not configured.

**Solution:**
```bash
# Option 1: Set via environment variable
$env:JWT_KEY="your_minimum_32_character_secret_key_here"

# Option 2: Set in appsettings.json
{
  "Jwt": {
    "Key": "your_minimum_32_character_secret_key_here"
  }
}

# Option 3: Use .env file
# Create .env in apiContact/ directory
JWT_KEY=your_minimum_32_character_secret_key_here

# Then run
dotnet run
```

---

### Error: `Unable to bind to http://127.0.0.1:5000`

**Cause:** Port 5000 is already in use.

**Solution:**
```bash
# Option 1: Use a different port
dotnet run --urls "http://localhost:5001"

# Option 2: Kill the process using port 5000
# Windows
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/macOS
lsof -i :5000
kill -9 <PID>

# Option 3: Check what's using the port
# Windows
Get-Process -Id (Get-NetTCPConnection -LocalPort 5000).OwningProcess
```

---

### Error: `The following constructor parameters did not have matching fixture data`

**Cause:** Dependency injection not properly configured.

**Solution:**
```csharp
// Ensure Program.cs has:
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
```

---

## Database Issues

### MongoDB Connection Failed

#### Error: `MongoDB connection refused at localhost:27017`

**Cause:** MongoDB service is not running.

**Solution:**
```bash
# Windows - Start MongoDB
# If installed as service
net start MongoDB

# Or using Chocolatey
choco upgrade mongodb-community

# macOS
brew services start mongodb-community

# Linux (Ubuntu)
sudo systemctl start mongod

# Docker
docker run -d -p 27017:27017 --name mongodb mongo:latest

# Verify connection
mongosh "mongodb://localhost:27017"
```

#### Error: `Unable to connect to MongoDB Atlas`

**Cause:** Network connectivity issue or incorrect connection string.

**Solution:**
```
1. Check connection string format:
   mongodb+srv://username:password@cluster.xxxxx.mongodb.net/dbname?retryWrites=true&w=majority

2. Verify credentials are URL-encoded
   Special chars like @ must be encoded as %40

3. Whitelist your IP in Atlas Console:
   - Go to Atlas Console
   - Network Access
   - Add IP Address (or 0.0.0.0/0 for development)

4. Test connection locally:
   mongosh "mongodb+srv://user:pass@cluster..."

5. Check firewall settings
```

#### Error: `Authentication failed`

**Cause:** Invalid MongoDB credentials or authentication database.

**Solution:**
```
1. Verify username and password in connection string
2. Check if user exists in MongoDB
3. Ensure using correct authentication database
4. For Atlas, use admin database by default

Connection string:
mongodb+srv://username:password@cluster.xxxxx.mongodb.net/admin?retryWrites=true&w=majority
```

---

### MongoDB In-Memory Fallback Not Working

**Cause:** Dependencies not installed.

**Solution:**
```bash
# Ensure MongoDB.Driver is installed
dotnet add package MongoDB.Driver

# If using in-memory, ensure it's configured in Program.cs:
builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        // Use in-memory
        var inMemoryDb = new InMemoryMongoDatabase();
        return inMemoryDb;
    }
    
    var client = new MongoClient(connectionString);
    return client.GetDatabase("chatdb");
});
```

---

## Redis Issues

### Redis Connection Failed

#### Error: `Failed to connect to Redis`

**Cause:** Redis service not running or misconfigured.

**Solution:**
```bash
# Start Redis
# Windows
redis-cli ping  # Check if running

# If not running, install and start
choco install redis-64
redis-server

# macOS
brew services start redis

# Linux
sudo systemctl start redis-server

# Docker
docker run -d -p 6379:6379 redis:latest

# Verify connection
redis-cli ping
# Expected response: PONG
```

#### Error: `Redis connection timeout`

**Cause:** Redis not accessible or firewall blocking.

**Solution:**
```bash
# Check Redis is running on port 6379
netstat -an | grep 6379

# Verify connection string
# Should be: localhost:6379 or 127.0.0.1:6379

# Test connection
redis-cli -h 127.0.0.1 -p 6379 ping

# Check firewall settings
```

---

### Redis Gracefully Disabled

If Redis is not configured, the app should continue working without caching.

If you get errors:
```csharp
// Check Program.cs has graceful fallback:
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });
}
else
{
    // Fallback to in-memory cache
    builder.Services.AddMemoryCache();
}
```

---

## Authentication Issues

### Error: `Invalid token` or `401 Unauthorized`

**Cause:** JWT token is invalid, expired, or missing.

**Solution:**
```bash
# 1. Ensure you're including the token in the request
curl -H "Authorization: Bearer <token>" http://localhost:5000/api/users/profile

# 2. Check token is valid
# Decode JWT at https://jwt.io/

# 3. Generate new token by logging in again
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}'

# 4. Check JWT_KEY matches both token generation and validation
# If you changed JWT_KEY, old tokens become invalid
```

### Error: `Password hashing failed` or `BCrypt exception`

**Cause:** BCrypt library issue or invalid password.

**Solution:**
```bash
# Ensure BCrypt.Net-Next is installed
dotnet add package BCrypt.Net-Next

# Ensure JWT_KEY is properly set
# Try with a new user registration
```

---

## API Endpoint Issues

### Error: `404 Not Found`

**Cause:** Endpoint doesn't exist or route is incorrect.

**Solution:**
```bash
# Check endpoint exists
# GET /api/users/profile
# Not: /api/user/profile or /users/profile

# Verify in controller:
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        // ...
    }
}

# View all routes in Swagger UI
http://localhost:5000/swagger
```

### Error: `405 Method Not Allowed`

**Cause:** Using wrong HTTP method (POST instead of GET, etc.).

**Solution:**
```bash
# Check HTTP method matches:
GET    /api/users/profile          ✓ Correct
POST   /api/users/profile          ✗ Wrong method
DELETE /api/users/{id}              ✓ Correct
GET    /api/users/{id}              ✗ Wrong method

# View correct method in Swagger UI
http://localhost:5000/swagger
```

### Error: `400 Bad Request`

**Cause:** Invalid request body or parameters.

**Solution:**
```bash
# 1. Check Content-Type header
-H "Content-Type: application/json"

# 2. Validate JSON format
# Use Swagger UI to test: http://localhost:5000/swagger

# 3. Check required fields
{
  "email": "user@example.com",      // Required
  "password": "SecurePass123",      // Required
  "firstName": "John",              // Optional
  "lastName": "Doe"                 // Optional
}

# 4. View validation error details
{
  "error": "Invalid request",
  "details": [
    {
      "field": "email",
      "message": "Invalid email format"
    }
  ]
}
```

### Error: `409 Conflict`

**Cause:** Resource already exists (e.g., duplicate email).

**Solution:**
```bash
# Check if user already exists
# Try with different email or delete existing record

# Example:
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"unique@example.com","password":"Pass@123"}'
```

---

## WebSocket Issues

### Connection Failed to SignalR Hub

#### Error: `WebSocket connection failed`

**Cause:** Hub URL incorrect or SignalR not enabled.

**Solution:**
```javascript
// Correct URL format
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/hubs/chat")  // ✓ Correct
  //.withUrl("http://localhost:5000/chat")     // ✗ Wrong
  .withAutomaticReconnect()
  .build();

connection.start()
  .then(() => console.log("Connected"))
  .catch(err => console.error("Connection error:", err));
```

#### Ensure SignalR is configured:

```csharp
// In Program.cs
builder.Services.AddSignalR();

var app = builder.Build();

// ...

app.MapHub<ChatHub>("/hubs/chat");  // Must match client URL
```

### No Messages Received via WebSocket

**Cause:** Client not listening for events.

**Solution:**
```javascript
// Set up event listener BEFORE sending
connection.on("ReceiveMessage", (message) => {
  console.log("New message:", message);
});

// Then invoke method
connection.invoke("SendMessage", {
  receiverId: "123",
  text: "Hello!"
})
.catch(err => console.error(err));
```

---

## Build Issues

### Error: `The project file could not be loaded`

**Cause:** Corrupted project file or XML syntax error.

**Solution:**
```bash
# 1. Check .csproj file syntax
# Open apiContact/apiContact.csproj

# 2. Restore and rebuild
dotnet clean
dotnet restore
dotnet build

# 3. Check Visual Studio/Code for XML errors
```

### Error: `CS1705: Assembly has a dependency on a higher version`

**Cause:** NuGet package version mismatch.

**Solution:**
```bash
# Update all packages
dotnet package update

# Or update specific package
dotnet package update Microsoft.AspNetCore.App

# Clean and restore
dotnet clean
dotnet restore
dotnet build
```

---

## Performance Issues

### Slow API Responses

**Cause:** N+1 queries, missing indexes, or inefficient logic.

**Solution:**
```bash
# 1. Enable query logging
# In appsettings.json:
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  }
}

# 2. Check database indexes
mongosh
> use chatdb
> db.messages.getIndexes()

# 3. Create missing indexes
db.messages.createIndex({ conversationId: 1, createdAt: -1 })

# 4. Use projection to get only needed fields
# 5. Implement pagination
# 6. Cache frequently accessed data
```

### High Memory Usage

**Cause:** Large collections loaded into memory.

**Solution:**
```bash
# 1. Use pagination instead of loading all records
# 2. Implement lazy loading
# 3. Clear cache periodically
# 4. Check for memory leaks in services

# Monitor memory
dotnet trace ps
dotnet trace collect --name apiContact.dll
```

---

## Deployment Issues

### Error: `Timeout waiting for SQL Server`

**Cause:** Database not accessible in production.

**Solution:**
```bash
# 1. Verify connection string is correct for production
# 2. Check firewall rules allow connection
# 3. Verify database user has permissions
# 4. Check network connectivity to database host

# Test from application server:
mongosh "mongodb+srv://user:pass@cluster.mongodb.net/dbname"
```

### Error: `CORS blocked`

**Cause:** Frontend and API running on different origins.

**Solution:**
```csharp
// In Program.cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder
            .WithOrigins("https://yourdomain.com", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

app.UseCors();
```

---

## Debug Mode

### Enable Detailed Error Messages

```csharp
// In Program.cs
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// In appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning"
    }
  }
}
```

### Enable Request/Response Logging

```csharp
// Custom middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Request: {Method} {Path}", 
        context.Request.Method, 
        context.Request.Path);
    
    await next();
    
    logger.LogInformation("Response: {Status}", context.Response.StatusCode);
});
```

---

## Getting Help

### Check Logs

```bash
# View application logs
# Windows Event Viewer: Applications and Services Logs

# Or check log files:
tail -f logs/app.log

# View Docker logs
docker logs <container_id>
```

### Enable Debug Mode

```bash
# Run with debug output
dotnet run --verbosity Debug

# Or set environment variable
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run
```

### Contact Support

- 📌 [GitHub Issues](https://github.com/Mostafa-SAID7/Chat-Api/issues)
- 💬 [GitHub Discussions](https://github.com/Mostafa-SAID7/Chat-Api/discussions)
- 📧 Email the maintainer

---

See also:
- [Setup Guide](./SETUP.md)
- [API Documentation](./API.md)
- [Development Guide](./DEVELOPMENT.md)
