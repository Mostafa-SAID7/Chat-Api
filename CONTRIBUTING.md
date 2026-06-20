# 🤝 Contributing Guide

Thank you for your interest in contributing to Chat API! This guide will help you get started.

---

## Code of Conduct

We are committed to providing a welcoming and inclusive environment. Please:

- Be respectful and professional
- Listen to different perspectives
- Focus on constructive feedback
- Report inappropriate behavior to maintainers

---

## Getting Started

### 1. Fork the Repository

Click the "Fork" button on GitHub to create your own copy.

### 2. Clone Your Fork

```bash
git clone https://github.com/YOUR_USERNAME/Chat-Api.git
cd Chat-Api
```

### 3. Add Upstream Remote

```bash
git remote add upstream https://github.com/Mostafa-SAID7/Chat-Api.git
```

### 4. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
```

Use descriptive branch names:
- `feature/add-user-blocking` ✓
- `fix/message-sorting-bug` ✓
- `docs/update-api-docs` ✓
- `feature/x` ✗

---

## Development Workflow

### Setup Local Environment

```bash
cd Chat-Api/apiContact
dotnet restore
dotnet user-secrets set JWT_KEY "your_secret_key_32_chars_minimum"
dotnet run
```

See [Setup Guide](./docs/SETUP.md) for detailed instructions.

### Making Changes

1. **Create a new feature branch** from `main`
2. **Make your changes** with meaningful commits
3. **Test your changes** locally
4. **Keep commits atomic** - one logical change per commit
5. **Write descriptive commit messages**

### Commit Message Format

Follow conventional commits:

```
type(scope): subject

body (optional)

footer (optional)
```

**Types:**
- `feat` - New feature
- `fix` - Bug fix
- `docs` - Documentation
- `style` - Code style (formatting, etc.)
- `refactor` - Code refactoring
- `test` - Adding or updating tests
- `chore` - Build, dependencies, etc.

**Examples:**
```
feat(auth): add two-factor authentication
fix(messages): resolve sorting issue on pagination
docs(api): update endpoint documentation
refactor(database): optimize query performance
test(contacts): add unit tests for contact service
```

---

## Code Standards

### .NET C# Standards

**Naming Conventions:**
```csharp
// Classes: PascalCase
public class UserService { }

// Methods: PascalCase
public async Task<User> GetUserAsync(string id) { }

// Properties: PascalCase
public string Email { get; set; }

// Private fields: camelCase with underscore
private readonly IRepository _repository;

// Constants: UPPER_SNAKE_CASE
public const int MAX_PAGE_SIZE = 100;

// Local variables: camelCase
var userName = "john_doe";
```

**Async/Await:**
```csharp
// Good: Always use async for I/O
public async Task<User> GetUserAsync(string id)
{
    return await _repository.GetByIdAsync(id);
}

// Bad: Sync method wrapping async
public User GetUser(string id)
{
    return _repository.GetByIdAsync(id).Result;
}
```

**Error Handling:**
```csharp
// Good: Custom exceptions with context
public class UserNotFoundException : Exception
{
    public UserNotFoundException(string userId) 
        : base($"User '{userId}' not found") { }
}

try
{
    var user = await GetUserAsync(userId);
    if (user == null)
        throw new UserNotFoundException(userId);
}
catch (UserNotFoundException ex)
{
    _logger.LogWarning(ex, "User lookup failed");
    return NotFound(new { message = ex.Message });
}

// Bad: Generic exceptions
if (user == null)
    throw new Exception("User not found");
```

**XML Documentation:**
```csharp
/// <summary>
/// Retrieves a user by their unique identifier.
/// </summary>
/// <param name="id">The unique user identifier.</param>
/// <returns>The user if found; otherwise null.</returns>
/// <exception cref="ArgumentNullException">Thrown when id is null.</exception>
public async Task<User> GetUserAsync(string id)
{
    // Implementation
}
```

### File Organization

```
Features/
├── FeatureName/
│   ├── Commands/
│   │   ├── CreateFeatureCommand.cs
│   │   ├── CreateFeatureCommandValidator.cs
│   │   └── CreateFeatureCommandHandler.cs
│   ├── Queries/
│   │   ├── GetFeatureQuery.cs
│   │   └── GetFeatureQueryHandler.cs
│   └── Events/
│       └── FeatureCreatedEvent.cs
```

---

## Testing

### Writing Tests

```csharp
[TestFixture]
public class UserServiceTests
{
    private UserService _userService;
    private Mock<IUserRepository> _mockRepository;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IUserRepository>();
        _userService = new UserService(_mockRepository.Object);
    }

    [Test]
    public async Task GetUserAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var userId = "123";
        var expectedUser = new User { Id = userId, Email = "user@example.com" };
        _mockRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _userService.GetUserAsync(userId);

        // Assert
        Assert.That(result, Is.EqualTo(expectedUser));
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }

    [Test]
    public async Task GetUserAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var userId = "";

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _userService.GetUserAsync(userId));
    }
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName=UserServiceTests"

# Run with verbose output
dotnet test --verbosity normal
```

**Before submitting a PR:**
- [ ] All tests pass
- [ ] Add tests for new features
- [ ] Update tests for bug fixes

---

## Pull Request Process

### Before Submitting

1. **Pull latest main branch**
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Run tests locally**
   ```bash
   dotnet test
   ```

3. **Format code**
   ```bash
   dotnet format
   ```

4. **Build without warnings**
   ```bash
   dotnet build /p:TreatWarningsAsErrors=true
   ```

### Submit Pull Request

1. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create PR on GitHub**
   - Use a clear, descriptive title
   - Reference related issues: `Closes #123`
   - Describe what changed and why
   - Include screenshots/GIFs if applicable

3. **PR Title Format**
   ```
   [Type] Brief description under 70 characters
   
   Examples:
   [Feature] Add user blocking functionality
   [Fix] Resolve message sorting bug
   [Docs] Update API documentation
   ```

### PR Description Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] New feature
- [ ] Bug fix
- [ ] Documentation update
- [ ] Breaking change

## How to Test
1. Step 1
2. Step 2
3. Expected result

## Checklist
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] No new warnings
- [ ] Commits are atomic
- [ ] Code follows project style

## Screenshots (if applicable)
<!-- Add screenshots for UI changes -->
```

---

## Review Process

Your PR will be reviewed by maintainers. They may:

1. **Request changes** - Please address feedback and re-request review
2. **Ask clarifying questions** - Help us understand your implementation
3. **Suggest improvements** - We collaborate to achieve the best solution
4. **Approve** - Your PR is ready to merge!

---

## Documentation

### Update Documentation

If your changes affect:
- **API endpoints** → Update [API.md](./docs/API.md)
- **Architecture** → Update [ARCHITECTURE.md](./docs/ARCHITECTURE.md)
- **Setup** → Update [SETUP.md](./docs/SETUP.md)
- **Database** → Update [DATABASE.md](./docs/DATABASE.md)

### README Updates

Update [README.md](./README.md) if:
- Adding new features
- Changing setup process
- Updating technology versions

---

## Common Contribution Types

### Adding a New Feature

1. Create feature branch
2. Implement feature in `Features/` folder
3. Add repository/service layer if needed
4. Create controller endpoint
5. Add unit tests
6. Update API documentation
7. Submit PR

### Fixing a Bug

1. Create branch: `fix/bug-description`
2. Add test that reproduces the bug
3. Fix the bug
4. Verify test passes
5. Update documentation if needed
6. Submit PR

### Updating Documentation

1. Create branch: `docs/change-description`
2. Update relevant markdown files
3. Preview on GitHub
4. Submit PR

---

## Reporting Issues

### Bug Reports

Include:
- Detailed description
- Steps to reproduce
- Expected behavior
- Actual behavior
- Environment (OS, .NET version, etc.)
- Error messages/logs

### Feature Requests

Include:
- Clear description
- Use case/motivation
- Possible implementation approach
- Any relevant examples

### Security Issues

**Do not create public issues for security vulnerabilities.**

Contact maintainers privately via email.

---

## Development Tools

### Recommended IDE

- **Visual Studio 2022** (Windows)
- **Visual Studio Code** with C# extensions
- **Rider** (JetBrains - paid)

### Extensions for VS Code

```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.vscode-dotnet-runtime",
    "ms-vscode.makefile-tools",
    "esbenp.prettier-vscode"
  ]
}
```

### Useful Commands

```bash
# Build
dotnet build

# Test
dotnet test

# Run
dotnet run

# Watch mode
dotnet watch run

# Format code
dotnet format

# Clean
dotnet clean

# Add NuGet package
dotnet add package <PackageName>

# Publish
dotnet publish -c Release
```

---

## Getting Help

- 📚 Read [Development Guide](./docs/DEVELOPMENT.md)
- 🏗️ Review [Architecture](./docs/ARCHITECTURE.md)
- 💬 Ask in [GitHub Discussions](https://github.com/Mostafa-SAID7/Chat-Api/discussions)
- 📖 Check existing issues

---

## Recognition

Contributors will be recognized in:
- README contributors section
- Release notes
- GitHub contributors page

Thank you for making Chat API better! 🎉

---

## License

By contributing, you agree that your contributions will be licensed under the same MIT License that covers the project.

---

Happy coding! 💻
