# 🚀 Setup Guide

## Prerequisites

- **[.NET 9 SDK](https://dotnet.microsoft.com/download)** - Required for running the API
- **[MongoDB](https://www.mongodb.com/try/download/community)** (Optional) - Database storage
- **[Redis](https://redis.io/download)** (Optional) - Caching layer
- **Git** - Version control

---

## 🚀 Quick Start Guide

### Option 1: Replit (Easiest - Cloud)

1. Click the **"Run"** button in Replit
2. Wait for the project to build and start
3. Access the API at the provided URL
4. Swagger UI: `{replit-url}/swagger`

**Features:**
- ✅ No local setup needed
- ✅ Works in browser
- ✅ Automatic environment setup
- ✅ One-click deployment

### Option 2: Local Docker (Recommended for Production-like)

```bash
# Clone repository
git clone https://github.com/Mostafa-SAID7/Chat-Api.git
cd Chat-Api

# Start with Docker
docker-compose up -d

# Access services
# API: http://localhost:5000/swagger
# MongoDB: http://localhost:8081
# Redis: http://localhost:8082
```

### Option 3: Local .NET (Fastest Development)

```bash
cd Chat-Api/apiContact
dotnet restore
dotnet run
```

API available at `http://localhost:5000`

---

## Local Development Setup

### Step 1: Clone Repository

```bash
git clone https://github.com/Mostafa-SAID7/Chat-Api.git
cd Chat-Api
```

### Step 2: Install Dependencies

```bash
cd apiContact
dotnet restore
```

### Step 3: Create Environment File

Create a `.env` file in the `apiContact/` directory:

```env
# JWT Configuration (required)
JWT_KEY=your_super_secret_key_minimum_32_characters_long_for_security

# MongoDB Configuration (optional)
MongoDB__ConnectionString=mongodb://localhost:27017/chatdb

# Redis Configuration (optional)
Redis__ConnectionString=localhost:6379
```

### Step 4: Configure Application Settings

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "mongodb://localhost:27017/chatdb"
  },
  "Jwt": {
    "Key": "your_super_secret_key_minimum_32_characters_long",
    "Issuer": "ChatApi",
    "Audience": "ChatApiClient",
    "ExpirationMinutes": 1440
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5000"]
  }
}
```

### Step 5: Run the Application

```bash
dotnet run
```

**Output:**
```
Now listening on: http://localhost:5000
Application started.
```

---

## Database Setup

### MongoDB

#### Option 1: Local MongoDB

**Windows:**
```bash
# Download and install MongoDB Community Edition
# https://www.mongodb.com/try/download/community

# Or use Chocolatey
choco install mongodb-community
```

**macOS:**
```bash
brew tap mongodb/brew
brew install mongodb-community
brew services start mongodb-community
```

**Linux (Ubuntu):**
```bash
curl -fsSL https://www.mongodb.org/static/pgp/server-4.4.asc | sudo apt-key add -
sudo apt-get install -y mongodb-org
sudo systemctl start mongod
```

#### Option 2: MongoDB Atlas (Cloud)

1. Go to https://www.mongodb.com/cloud/atlas
2. Create a free account
3. Create a cluster
4. Get connection string
5. Set `MongoDB__ConnectionString` environment variable

```env
MongoDB__ConnectionString=mongodb+srv://username:password@cluster0.xxxxx.mongodb.net/chatdb?retryWrites=true&w=majority
```

#### Option 3: Docker

```bash
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

#### Verify Connection

```bash
# Using MongoDB CLI
mongosh "mongodb://localhost:27017"

# Or check if port 27017 is listening
netstat -an | grep 27017
```

### Redis

#### Option 1: Local Redis

**Windows:**
```bash
# Install from Microsoft's fork
choco install redis-64

# Or download from: https://github.com/microsoftarchive/redis/releases
```

**macOS:**
```bash
brew install redis
brew services start redis
```

**Linux (Ubuntu):**
```bash
sudo apt-get install redis-server
sudo systemctl start redis-server
```

#### Option 2: Docker

```bash
docker run -d -p 6379:6379 --name redis redis:latest
```

#### Verify Connection

```bash
# Using redis-cli
redis-cli ping
# Response: PONG
```

---

## Running with Docker Compose

Create `docker-compose.yml` in project root:

```yaml
version: '3.8'

services:
  api:
    build:
      context: ./apiContact
      dockerfile: Dockerfile
    ports:
      - "5000:5000"
    environment:
      JWT_KEY: ${JWT_KEY}
      MongoDB__ConnectionString: mongodb://mongo:27017/chatdb
      Redis__ConnectionString: redis:6379
    depends_on:
      - mongo
      - redis

  mongo:
    image: mongo:latest
    ports:
      - "27017:27017"
    volumes:
      - mongo_data:/data/db
    environment:
      MONGO_INITDB_DATABASE: chatdb

  redis:
    image: redis:latest
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

volumes:
  mongo_data:
  redis_data:
```

Run with Docker Compose:

```bash
docker-compose up -d
```

---

## API Access

### Swagger UI

Once running, access the interactive API documentation:

```
http://localhost:5000/swagger
```

### Test Endpoints

**Register:**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@1234",
    "firstName": "Test",
    "lastName": "User"
  }'
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "507f1f77bcf86cd799439011",
  "email": "test@example.com"
}
```

**Login:**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@1234"
  }'
```

**Get Profile:**
```bash
curl -X GET http://localhost:5000/api/users/profile \
  -H "Authorization: Bearer <your_token>"
```

---

## Development Environment

### Watch Mode

Auto-reload on file changes:

```bash
dotnet watch run
```

### Code Generation

Generate code from features:

```bash
dotnet user-secrets set JWT_KEY "your_secret_key"
```

### Database Migrations (if using EF Core)

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Troubleshooting

### Port Already in Use

If port 5000 is already in use:

```bash
# Option 1: Use different port
dotnet run --urls "http://localhost:5001"

# Option 2: Kill process using port 5000
# Windows
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/macOS
lsof -i :5000
kill -9 <PID>
```

### MongoDB Connection Failed

```
Error: Failed to connect to MongoDB
```

**Solutions:**
1. Ensure MongoDB is running: `mongosh`
2. Check connection string format
3. Verify firewall settings
4. Check MongoDB service status

### Redis Connection Failed

```
Error: Failed to connect to Redis
```

**Solutions:**
1. Ensure Redis is running: `redis-cli ping`
2. Check port 6379 is accessible
3. Verify firewall settings
4. Check Redis service status

### JWT_KEY Not Set

```
Error: JWT_KEY environment variable is required
```

**Solution:**
```bash
# Set in .env file
JWT_KEY=your_minimum_32_character_secret_key_here
```

### CORS Errors

```
Access to XMLHttpRequest at 'http://localhost:5000/...' from origin 'http://localhost:3000' has been blocked
```

**Solution:** Update `appsettings.json`:
```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5000"
  ]
}
```

---

## Production Deployment

### Environment Checklist

- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Use strong JWT_KEY (min 32 chars)
- [ ] Configure MongoDB Atlas or managed instance
- [ ] Configure Redis cluster
- [ ] Enable HTTPS/SSL
- [ ] Configure CORS properly
- [ ] Set up logging and monitoring
- [ ] Configure backups
- [ ] Set up CI/CD pipeline

### Deploy to Azure

```bash
# Login to Azure
az login

# Create resource group
az group create --name ChatApiRG --location eastus

# Create App Service Plan
az appservice plan create --name ChatApiPlan --resource-group ChatApiRG --sku B2

# Create Web App
az webapp create --resource-group ChatApiRG --plan ChatApiPlan --name chat-api

# Deploy
dotnet publish -c Release
az webapp deployment source config-zip --resource-group ChatApiRG --name chat-api --src bin/Release/net9.0/publish.zip
```

### Deploy to Docker

Create `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["apiContact/apiContact.csproj", "apiContact/"]
RUN dotnet restore "apiContact/apiContact.csproj"
COPY . .
RUN dotnet build "apiContact/apiContact.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "apiContact/apiContact.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "apiContact.dll"]
```

Build and run:

```bash
docker build -t chat-api .
docker run -p 5000:5000 \
  -e JWT_KEY="your_secret_key" \
  -e MongoDB__ConnectionString="mongodb://..." \
  chat-api
```

---

## Next Steps

1. Read [API Documentation](./API.md)
2. Check [Architecture](./ARCHITECTURE.md)
3. Review [Development Guide](./DEVELOPMENT.md)
4. Explore WebSocket integration in [Database Guide](./DATABASE.md)

---

See also:
- [Troubleshooting](./TROUBLESHOOTING.md)
- [Contributing](./CONTRIBUTING.md)
