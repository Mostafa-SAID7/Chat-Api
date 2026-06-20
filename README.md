# 💬 Chat API

<div align="center">

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-9-512BD4?logo=dotnet)](https://dotnet.microsoft.com/apps/aspnet)

A modern, real-time chat REST API built with **ASP.NET Core 9**, **SignalR**, **MongoDB**, and **Redis** caching.

[Quick Start](#-quick-start) • [Documentation](./docs) • [Architecture](./docs/ARCHITECTURE.md) • [Contributing](./CONTRIBUTING.md)

</div>

---

## ✨ Features

- 🚀 **Real-time Messaging** - WebSocket support via SignalR
- 🔐 **Secure Authentication** - JWT tokens with BCrypt hashing
- 💾 **Flexible Database** - MongoDB with in-memory fallback
- ⚡ **Performance** - Redis caching (optional)
- 📁 **File Storage** - Local blob storage for media
- 📚 **API Documentation** - Swagger UI included
- 🏗️ **Clean Architecture** - Repository + UoW + MediatR CQRS pattern
- 🔄 **Graceful Degradation** - Works without external services

---

## 🛠️ Tech Stack

| Component | Technology |
|-----------|-----------|
| **Framework** | ASP.NET Core 9 |
| **Architecture** | Repository + Unit of Work + MediatR (CQRS) |
| **Real-time** | SignalR WebSockets |
| **Database** | MongoDB (with in-memory fallback) |
| **Caching** | Redis (optional) |
| **Authentication** | JWT Bearer + BCrypt |
| **API Docs** | Swagger/OpenAPI |
| **Storage** | Local file system |

---

## 🚀 Quick Start

### Prerequisites

- .NET 9 SDK
- MongoDB (optional - will use in-memory if not configured)
- Redis (optional)

### Installation

```bash
git clone https://github.com/Mostafa-SAID7/Chat-Api.git
cd Chat-Api/apiContact
dotnet restore
```

### Running Locally

```bash
dotnet run
```

The API will start on **http://localhost:5000**

Access Swagger UI: **http://localhost:5000/swagger**

### Environment Configuration

Create a `.env` file or set these environment variables:

```env
JWT_KEY=your_secret_key_minimum_32_characters_long
MongoDB__ConnectionString=mongodb://localhost:27017/chatdb
Redis__ConnectionString=localhost:6379
```

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `JWT_KEY` | Secret for JWT signing (min 32 chars) | ✅ Yes | N/A |
| `MongoDB__ConnectionString` | MongoDB connection | ❌ No | In-memory DB |
| `Redis__ConnectionString` | Redis connection | ❌ No | Disabled |

---

## 📚 Documentation

Full documentation is available in the [`/docs`](./docs) directory:

- **[ARCHITECTURE.md](./docs/ARCHITECTURE.md)** - System design and components
- **[API.md](./docs/API.md)** - API endpoints and usage
- **[SETUP.md](./docs/SETUP.md)** - Detailed setup instructions
- **[DEVELOPMENT.md](./docs/DEVELOPMENT.md)** - Development guide
- **[DATABASE.md](./docs/DATABASE.md)** - Database schema and models
- **[TROUBLESHOOTING.md](./docs/TROUBLESHOOTING.md)** - Common issues and fixes

---

## 🔌 API Endpoints

### Chat Hub (WebSocket)

**Base URL:** `ws://localhost:5000/hubs/chat`

### Core Endpoints

```
POST   /api/auth/register       Register new user
POST   /api/auth/login          Login and get JWT
GET    /api/contacts            Get all contacts
POST   /api/contacts            Create new contact
GET    /api/messages            Get conversation history
POST   /api/messages            Send message
```

See [API Documentation](./docs/API.md) for complete endpoint reference.

---

## 🏗️ Project Structure

```
apiContact/
├── Controllers/          # API endpoints
├── Features/            # Feature-specific logic (CQRS)
├── Hubs/               # SignalR WebSocket hubs
├── Services/           # Business logic services
├── Models/             # Data models/entities
├── Data/               # Database context & repositories
├── Middleware/         # Custom middleware
├── Mappings/           # AutoMapper profiles
├── Utilities/          # Helper utilities
├── Properties/         # Project properties
├── appsettings.json    # Configuration
└── Program.cs          # Application startup
```

---

## 🔐 Authentication

The API uses **JWT Bearer tokens** for authentication:

1. Register or login to get a token
2. Include token in request header: `Authorization: Bearer <token>`
3. Token expires after 24 hours

Example:
```bash
curl -H "Authorization: Bearer eyJhbGc..." http://localhost:5000/api/contacts
```

---

## 💡 Usage Example

### Register & Login

```bash
# Register
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"SecurePass123"}'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"SecurePass123"}'
```

### Send Message via WebSocket

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/hubs/chat")
  .withAutomaticReconnect()
  .build();

connection.start();

connection.invoke("SendMessage", {
  contactId: "123",
  text: "Hello!",
  timestamp: new Date()
});
```

---

## 🧪 Testing

```bash
cd apiContact
dotnet test
```

---

## 📦 Development

### Build

```bash
dotnet build
```

### Watch Mode

```bash
dotnet watch run
```

See [DEVELOPMENT.md](./docs/DEVELOPMENT.md) for more details.

---

## 🤝 Contributing

We welcome contributions! Please see [CONTRIBUTING.md](./CONTRIBUTING.md) for guidelines.

### Steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📝 License

This project is licensed under the MIT License - see [LICENSE.txt](./LICENSE.txt) for details.

---

## 🤔 FAQ

**Q: Do I need MongoDB to run this?**  
A: No, it includes an in-memory fallback. MongoDB is optional.

**Q: Can I use this in production?**  
A: Yes, with proper MongoDB and Redis setup. See [SETUP.md](./docs/SETUP.md).

**Q: How do I enable real-time chat?**  
A: Connect to the SignalR hub at `/hubs/chat` for real-time messaging.

---

## 📞 Support

For issues, questions, or suggestions:
- 📌 [Open an Issue](https://github.com/Mostafa-SAID7/Chat-Api/issues)
- 💬 [Start a Discussion](https://github.com/Mostafa-SAID7/Chat-Api/discussions)

---

<div align="center">

**Made with ❤️ by the Chat API team**

[⬆ Back to top](#-chat-api)

</div>