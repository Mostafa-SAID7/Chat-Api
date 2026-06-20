# Chat API

A real-time chat REST API built with ASP.NET Core (.NET 9), SignalR, MongoDB (with in-memory fallback), and Redis (optional).

## Architecture

- **Framework**: ASP.NET Core (.NET 9)
- **Pattern**: Repository + Unit of Work + MediatR CQRS
- **Real-time**: SignalR WebSockets (`/hubs/chat`)
- **Auth**: JWT Bearer tokens (BCrypt password hashing)
- **Database**: MongoDB with automatic in-memory fallback when no connection string is provided
- **Cache**: Redis (optional, gracefully skipped if not configured)
- **Storage**: Local blob storage (`wwwroot/uploads`)
- **Docs**: Swagger UI at `/swagger`

## Running

The app runs via `dotnet run` in the `apiContact/` directory and listens on port 5000.

## Environment Variables / Secrets

| Key | Description |
|-----|-------------|
| `JWT_KEY` | Secret key for signing JWT tokens (min 32 chars) |
| `MongoDB__ConnectionString` | MongoDB connection string (optional — falls back to in-memory) |
| `Redis__ConnectionString` | Redis connection string (optional) |

## User preferences

- Keep in-memory fallback for MongoDB so the app works out of the box without external services.
