# 💻 Development Guide

## Getting Started for Developers

### Prerequisites

- .NET 9 SDK
- Visual Studio Code or Visual Studio 2022
- Git
- MongoDB (local or Atlas)
- Redis (optional)

---

## Project Structure Overview

```
apiContact/
├── Controllers/              # HTTP API endpoints
│   ├── AuthController.cs
│   ├── ContactsController.cs
│   ├── MessagesController.cs
│   └── UsersController.cs
│
├── Features/                 # CQRS Features (Commands & Queries)
│   ├── Auth/
│   │   ├── Commands/
│   │   │   ├── RegisterCommand.cs
│   │   │   ├── RegisterCommandHandler.cs
│   │   │   ├── LoginCommand.cs
│   │   │   └── LoginCommandHandler.cs
│   │   └── Queries/
│   │
│   ├── Contacts/
│   │   ├── Commands/
│   │   └── Queries/
│   │
│   └── Messages/
│       ├── Commands/
│       └── Queries/
│
├── Hubs/                     # SignalR WebSocket Hubs
│   └── ChatHub.cs
│
├── Services/                 # Business Logic Services
│   ├── AuthService.cs
│   ├── MessageService.cs
│   ├── ContactService.cs
│   └── NotificationService.cs
│
├── Models/                   # Data Models & Entities
│   ├── User.cs
│   ├── Contact.cs
│   ├── Message.cs
│   └── DTOs/
│       ├── UserDto.cs
│       ├── MessageDto.cs
│       └── ContactDto.cs
│
├── Data/                     # Database Layer
│   ├── ApplicationDbContext.cs
│   ├── Repositories/
│   │   ├── IRepository.cs
│   │   ├── Repository.cs
│   │   ├── IContactRepository.cs
│   │   ├── IMessageRepository.cs
│   │   └── IUserRepository.cs
│   └── UnitOfWork/
│       ├── IUnitOfWork.cs
│       └── UnitOfWork.cs
│
├── Middleware/               # Custom Middleware
│   ├── ExceptionHandlingMiddleware.cs
│   ├── LoggingMiddleware.cs
│   └── AuthenticationMiddleware.cs
│
├── Mappings/                 # AutoMapper Profiles
│   ├── UserMappingProfile.cs
│   ├── ContactMappingProfile.cs
│   └── MessageMappingProfile.cs
│
├── Utilities/                # Helper Utilities
│   ├── PasswordHasher.cs
│   ├── JwtTokenGenerator.cs
│   └── ValidationHelpers.cs
│
├── Program.cs                # Application Startup
├── appsettings.json          # Configuration
└── apiContact.csproj         # Project File
```

---

## Development Workflow

### 1. Setting Up Your Local Environment

```bash
# Clone the repository
git clone https://github.com/Mostafa-SAID7/Chat-Api.git
cd Chat-Api/apiContact

# Restore dependencies
dotnet restore

# Set JWT secret
dotnet user-secrets set JWT_KEY "your_minimum_32_character_secret_key"

# Run the application
dotnet run
```

### 2. Running in Watch Mode

Auto-reload when files change:

```bash
dotnet watch run
```

### 3. Building the Project

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release
```

---

## Adding a New Feature

### Step 1: Create the Database Model

`Models/NewEntity.cs`:
```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class NewEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("description")]
    public string Description { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;
}
```

### Step 2: Create DTOs

`Models/DTOs/NewEntityDto.cs`:
```csharp
public class CreateNewEntityDto
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class NewEntityDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Step 3: Create Repository Interface

`Data/Repositories/INewEntityRepository.cs`:
```csharp
using apiContact.Models;

namespace apiContact.Data.Repositories
{
    public interface INewEntityRepository : IRepository<NewEntity>
    {
        Task<NewEntity> GetByNameAsync(string name);
        Task<IEnumerable<NewEntity>> GetActiveEntitiesAsync();
    }
}
```

### Step 4: Implement Repository

`Data/Repositories/NewEntityRepository.cs`:
```csharp
using apiContact.Models;
using MongoDB.Driver;

namespace apiContact.Data.Repositories
{
    public class NewEntityRepository : Repository<NewEntity>, INewEntityRepository
    {
        public NewEntityRepository(IMongoDatabase database) 
            : base(database, "newentities")
        {
        }

        public async Task<NewEntity> GetByNameAsync(string name)
        {
            var filter = Builders<NewEntity>.Filter.Eq(x => x.Name, name);
            return await Collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<NewEntity>> GetActiveEntitiesAsync()
        {
            var filter = Builders<NewEntity>.Filter.Eq(x => x.IsDeleted, false);
            return await Collection.Find(filter).ToListAsync();
        }
    }
}
```

### Step 5: Update Unit of Work

`Data/UnitOfWork/IUnitOfWork.cs`:
```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IContactRepository Contacts { get; }
    IMessageRepository Messages { get; }
    INewEntityRepository NewEntities { get; }  // Add this
    Task SaveChangesAsync();
}
```

### Step 6: Create CQRS Commands/Queries

`Features/NewEntity/Commands/CreateNewEntityCommand.cs`:
```csharp
using MediatR;

namespace apiContact.Features.NewEntity.Commands
{
    public class CreateNewEntityCommand : IRequest<NewEntityDto>
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
```

`Features/NewEntity/Commands/CreateNewEntityCommandHandler.cs`:
```csharp
using MediatR;
using apiContact.Models;
using apiContact.Data;
using AutoMapper;

namespace apiContact.Features.NewEntity.Commands
{
    public class CreateNewEntityCommandHandler 
        : IRequestHandler<CreateNewEntityCommand, NewEntityDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateNewEntityCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<NewEntityDto> Handle(
            CreateNewEntityCommand request, 
            CancellationToken cancellationToken)
        {
            var entity = new NewEntity
            {
                Name = request.Name,
                Description = request.Description
            };

            var createdEntity = await _unitOfWork.NewEntities.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<NewEntityDto>(createdEntity);
        }
    }
}
```

### Step 7: Create AutoMapper Profile

`Mappings/NewEntityMappingProfile.cs`:
```csharp
using AutoMapper;
using apiContact.Models;

namespace apiContact.Mappings
{
    public class NewEntityMappingProfile : Profile
    {
        public NewEntityMappingProfile()
        {
            CreateMap<NewEntity, NewEntityDto>().ReverseMap();
            CreateMap<CreateNewEntityDto, NewEntity>();
        }
    }
}
```

### Step 8: Create Controller

`Controllers/NewEntitiesController.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using apiContact.Features.NewEntity.Commands;

namespace apiContact.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NewEntitiesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NewEntitiesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateNewEntityCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(Create), result);
        }
    }
}
```

---

## Testing

### Unit Tests Example

`Tests/Features/Auth/RegisterCommandHandlerTests.cs`:
```csharp
using Xunit;
using Moq;
using MediatR;
using apiContact.Features.Auth.Commands;

namespace apiContact.Tests.Features.Auth
{
    public class RegisterCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WithValidData_ReturnsToken()
        {
            // Arrange
            var command = new RegisterCommand
            {
                Email = "test@example.com",
                Password = "TestPass123",
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            // var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            // Assert.NotNull(result.Token);
        }

        [Fact]
        public async Task Handle_WithExistingEmail_ThrowsException()
        {
            // Test logic
        }
    }
}
```

Run tests:
```bash
dotnet test
```

---

## Code Standards

### Naming Conventions

- **Classes:** PascalCase (e.g., `UserService`)
- **Methods:** PascalCase (e.g., `GetUserById`)
- **Properties:** PascalCase (e.g., `UserId`)
- **Private fields:** camelCase with underscore (e.g., `_repository`)
- **Constants:** UPPER_SNAKE_CASE (e.g., `MAX_PAGE_SIZE`)

### File Organization

```
Feature/
├── Commands/
│   ├── CreateXxxCommand.cs
│   └── CreateXxxCommandHandler.cs
├── Queries/
│   ├── GetXxxQuery.cs
│   └── GetXxxQueryHandler.cs
└── Validators/
    └── CreateXxxValidator.cs
```

### Async/Await Pattern

Always use async for I/O operations:

```csharp
// Good
public async Task<User> GetUserAsync(string id)
{
    return await _repository.GetByIdAsync(id);
}

// Bad
public User GetUser(string id)
{
    return _repository.GetById(id).Result;
}
```

### Error Handling

Use custom exceptions:

```csharp
public class UserNotFoundException : Exception
{
    public UserNotFoundException(string userId) 
        : base($"User {userId} not found")
    {
    }
}

// Usage
if (user == null)
    throw new UserNotFoundException(userId);
```

---

## Debugging

### Debug in VS Code

Add `.vscode/launch.json`:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (web)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/apiContact/bin/Debug/net9.0/apiContact.dll",
            "args": [],
            "cwd": "${workspaceFolder}/apiContact",
            "stopAtEntry": false,
            "serverReadyAction": {
                "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
                "uriFormat": "{1}",
                "action": "openExternally"
            }
        }
    ]
}
```

### Logging

Use built-in logging:

```csharp
public class UserService
{
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public async Task<User> GetUserAsync(string id)
    {
        _logger.LogInformation("Getting user {UserId}", id);
        
        try
        {
            return await _repository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            throw;
        }
    }
}
```

---

## Committing Code

### Commit Messages

Follow conventional commits:

```
feat: add new chat feature
fix: resolve message sorting issue
docs: update API documentation
refactor: simplify authentication logic
test: add unit tests for contact service
chore: update dependencies
```

### Pre-commit Checks

```bash
# Format code
dotnet format

# Build project
dotnet build

# Run tests
dotnet test

# Commit
git commit -m "feat: add user blocking feature"
```

---

## Performance Tips

1. **Use async/await** - Never block threads
2. **Index frequently queried fields** - MongoDB indexes
3. **Cache with Redis** - Reduce database calls
4. **Pagination** - Don't return all records
5. **Lazy loading** - Load related entities on demand
6. **Connection pooling** - Reuse database connections

---

## Security Best Practices

1. **Validate input** - Always validate user input
2. **Hash passwords** - Use BCrypt, never plain text
3. **Secure tokens** - Use HTTPS in production
4. **CORS** - Configure properly for your domain
5. **Rate limiting** - Prevent brute force attacks
6. **Dependency scanning** - Check for vulnerabilities

```bash
dotnet list package --vulnerable
```

---

## Useful Commands

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run

# Watch mode
dotnet watch run

# Run tests
dotnet test

# Format code
dotnet format

# Publish
dotnet publish -c Release

# Add NuGet package
dotnet add package <PackageName>

# Remove NuGet package
dotnet remove package <PackageName>

# Clean
dotnet clean
```

---

## Resources

- [Microsoft Docs - ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [MongoDB C# Driver](https://docs.mongodb.com/drivers/csharp/)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction)

---

See also:
- [Architecture Guide](./ARCHITECTURE.md)
- [API Documentation](./API.md)
- [Database Schema](./DATABASE.md)
