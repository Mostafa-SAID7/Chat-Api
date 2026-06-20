# 🏗️ Architecture

## Overview

Chat API follows a **layered architecture** with **CQRS** pattern using **MediatR** for command/query separation, combined with **Repository** and **Unit of Work** patterns for data access.

```
┌─────────────────────────────────────────────┐
│         Presentation Layer                  │
│   (Controllers, SignalR Hubs, Swagger)      │
└────────────────┬────────────────────────────┘
                 │
┌────────────────┴────────────────────────────┐
│      Application Layer (Features)           │
│  (CQRS Commands, Queries, Handlers)         │
│         (MediatR Pipeline)                  │
└────────────────┬────────────────────────────┘
                 │
┌────────────────┴────────────────────────────┐
│      Business Logic Layer (Services)        │
│  (Authentication, Messaging, Validation)    │
└────────────────┬────────────────────────────┘
                 │
┌────────────────┴────────────────────────────┐
│     Data Access Layer (Repositories)        │
│  (Unit of Work, Repository Pattern)         │
└────────────────┬────────────────────────────┘
                 │
┌────────────────┴────────────────────────────┐
│      Infrastructure Layer                   │
│ (Database, Cache, File Storage, Auth)       │
└─────────────────────────────────────────────┘
```

---

## Components

### 1. **Controllers** (`Controllers/`)

RESTful endpoints for HTTP requests:

- **AuthController** - User registration, login, token refresh
- **ContactsController** - CRUD operations for contacts
- **MessagesController** - Message operations
- **UsersController** - User profile management

### 2. **SignalR Hubs** (`Hubs/`)

Real-time WebSocket communication:

- **ChatHub** - Main chat functionality
  - Broadcasting messages
  - User connection/disconnection
  - Typing indicators
  - Online status updates

### 3. **Features** (`Features/`)

CQRS pattern implementation using MediatR:

```
Features/
├── Auth/
│   ├── Commands/
│   │   ├── RegisterCommand
│   │   ├── LoginCommand
│   │   └── RefreshTokenCommand
│   ├── Queries/
│   │   └── GetUserQuery
│   └── Handlers/
├── Contacts/
│   ├── Commands/
│   ├── Queries/
│   └── Handlers/
├── Messages/
│   ├── Commands/
│   ├── Queries/
│   └── Handlers/
```

**Benefits:**
- Clear separation of concerns
- Easy to test
- Scalable command/query handling
- Built-in middleware pipeline

### 4. **Services** (`Services/`)

Business logic:

- **AuthService** - JWT token generation, password hashing
- **MessageService** - Message processing, validation
- **ContactService** - Contact management logic
- **NotificationService** - Real-time notifications

### 5. **Data Layer** (`Data/`)

Database abstraction:

```
Data/
├── ApplicationDbContext          # Main DbContext
├── Repositories/
│   ├── IRepository<T>           # Generic interface
│   ├── Repository<T>            # Generic implementation
│   ├── IContactRepository       # Specific interfaces
│   ├── IMessageRepository
│   └── ...
├── UnitOfWork/
│   ├── IUnitOfWork              # UoW interface
│   └── UnitOfWork               # UoW implementation
└── Configurations/              # Entity configurations
```

**Unit of Work Pattern:**
- Manages all repositories
- Ensures consistent transactions
- Single SaveChanges() for multiple operations

### 6. **Models** (`Models/`)

Data models:

- **User** - User accounts, authentication
- **Contact** - User contacts/relationships
- **Message** - Chat messages
- **Attachment** - Message attachments
- **DTOs** - Data transfer objects

### 7. **Middleware** (`Middleware/`)

Custom request/response processing:

- **ExceptionHandlingMiddleware** - Global error handling
- **AuthenticationMiddleware** - Token validation
- **LoggingMiddleware** - Request/response logging
- **ValidationMiddleware** - Input validation

### 8. **Mappings** (`Mappings/`)

AutoMapper profiles for DTO conversion:

- Model → DTO mapping
- DTO → Model mapping
- Nested object mapping

### 9. **Utilities** (`Utilities/`)

Helper functions:

- **PasswordHasher** - BCrypt hashing
- **JwtTokenGenerator** - Token creation
- **ValidationHelpers** - Input validation
- **FileStorage** - Blob storage operations

---

## Data Flow

### Authentication Flow

```
1. User Registration/Login
   ↓
2. AuthController receives request
   ↓
3. MediatR dispatches RegisterCommand/LoginCommand
   ↓
4. Command Handler validates input
   ↓
5. AuthService processes auth logic
   ↓
6. Repository saves/retrieves user data
   ↓
7. JWT token generated
   ↓
8. Response returned to client
```

### Message Flow

```
1. Client connects to ChatHub via WebSocket
   ↓
2. ChatHub.OnConnectedAsync() triggers
   ↓
3. User added to connection group
   ↓
4. Client sends message via SendMessage
   ↓
5. MediatR dispatches SendMessageCommand
   ↓
6. MessageService validates & processes
   ↓
7. Message stored in MongoDB
   ↓
8. Message broadcasted via SignalR
   ↓
9. Connected clients receive update
```

---

## Database Schema

### Users Collection

```json
{
  "_id": "ObjectId",
  "Email": "string",
  "PasswordHash": "string",
  "FirstName": "string",
  "LastName": "string",
  "Avatar": "string (URL)",
  "IsDeleted": "bool",
  "CreatedAt": "DateTime",
  "UpdatedAt": "DateTime"
}
```

### Contacts Collection

```json
{
  "_id": "ObjectId",
  "UserId": "ObjectId",
  "ContactUserId": "ObjectId",
  "Nickname": "string",
  "CreatedAt": "DateTime",
  "IsBlocked": "bool"
}
```

### Messages Collection

```json
{
  "_id": "ObjectId",
  "SenderId": "ObjectId",
  "ReceiverId": "ObjectId",
  "Text": "string",
  "Attachments": ["string (URLs)"],
  "IsDeleted": "bool",
  "IsRead": "bool",
  "CreatedAt": "DateTime",
  "UpdatedAt": "DateTime"
}
```

---

## External Services

### MongoDB

- **Primary Storage** - All persistent data
- **Fallback** - In-memory if not configured
- **Collections** - Users, Contacts, Messages, Attachments

### Redis

- **Cache Layer** - User sessions, online status
- **Message Queue** - Optional message queuing
- **Optional** - Gracefully disabled if not configured

### SignalR

- **Real-time Communication** - WebSocket connections
- **Hub Groups** - User-specific message routing
- **Connection Tracking** - Online status management

---

## Design Patterns Used

### 1. Repository Pattern
- Abstracts data access
- Easy to mock for testing
- Supports multiple databases

### 2. Unit of Work Pattern
- Manages all repositories
- Coordinates transactions
- Maintains object graph

### 3. CQRS (Command Query Responsibility Segregation)
- Separates read (queries) from write (commands)
- Optimized handlers for each operation
- Easier scaling

### 4. Dependency Injection
- Loose coupling
- Easy testing
- Configuration in `Program.cs`

### 5. Middleware Chain
- Cross-cutting concerns
- Centralized logging/error handling
- Extensible request pipeline

### 6. DTO Pattern
- Decouples API from internal models
- Allows flexible response shapes
- Reduces over-fetching

---

## Error Handling

```
Exception thrown
   ↓
ExceptionHandlingMiddleware catches
   ↓
Determines error type
   ↓
Returns appropriate HTTP status
   ↓
Logs error details
   ↓
Returns JSON error response to client
```

**HTTP Status Codes:**
- `200` - Success
- `201` - Created
- `400` - Bad Request
- `401` - Unauthorized
- `403` - Forbidden
- `404` - Not Found
- `409` - Conflict
- `500` - Server Error

---

## Security

### Authentication
- **JWT Bearer tokens** - Stateless authentication
- **BCrypt hashing** - Password security
- **Token expiration** - 24-hour lifetime

### Authorization
- **Role-based access control** - User/Admin roles
- **Resource ownership** - Users can only access their data
- **Soft delete** - Data recovery capability

### Input Validation
- **DTO validation** - FluentValidation
- **CORS** - Cross-origin control
- **Rate limiting** - Optional throttling

---

## Performance Optimizations

1. **Caching** - Redis for frequently accessed data
2. **Lazy Loading** - On-demand entity loading
3. **Pagination** - Large dataset handling
4. **Indexing** - MongoDB indexes on frequent queries
5. **Connection Pooling** - Database connection reuse
6. **Async/Await** - Non-blocking operations

---

## Scalability Considerations

1. **Horizontal Scaling** - Stateless API
2. **Database Sharding** - MongoDB sharding support
3. **Load Balancing** - Multiple API instances
4. **Message Queuing** - Background job processing
5. **Distributed Caching** - Redis clustering
6. **API Gateway** - Route management and throttling

---

## Development Workflow

```
Feature Request
   ↓
Create Feature Branch
   ↓
Implement Feature (TDD)
   ↓
Create Command/Query Handler
   ↓
Implement Repository Methods
   ↓
Write Unit Tests
   ↓
Code Review
   ↓
Merge to Main
   ↓
Deploy to Production
```

---

See also:
- [API Documentation](./API.md)
- [Development Guide](./DEVELOPMENT.md)
- [Database Schema](./DATABASE.md)
