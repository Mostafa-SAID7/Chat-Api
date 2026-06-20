# 📚 Documentation Index

Welcome to Chat API Documentation! Here you'll find everything you need to understand, develop, and deploy the Chat API.

---

## 🚀 Getting Started

Start here if you're new to the project:

1. **[README.md](../README.md)** - Project overview and features
2. **[SETUP.md](./SETUP.md)** - Local development setup and installation
3. **[Quick Start](#quick-start)** - Get running in 5 minutes

---

## 📖 Core Documentation

### For Users & API Consumers

- **[API.md](./API.md)** - Complete REST API and WebSocket endpoint reference
  - Authentication endpoints
  - User management
  - Contacts management
  - Messages and conversations
  - WebSocket/SignalR events

### For Developers

- **[ARCHITECTURE.md](./ARCHITECTURE.md)** - System design and architecture
  - Layered architecture overview
  - Component descriptions
  - CQRS pattern implementation
  - Data models and relationships
  - Design patterns used

- **[DEVELOPMENT.md](./DEVELOPMENT.md)** - Development guide
  - Project structure
  - Adding new features step-by-step
  - Code standards and conventions
  - Testing guidelines
  - Debugging tips

- **[DATABASE.md](./DATABASE.md)** - Database documentation
  - MongoDB collections schema
  - Indexes and optimization
  - Query examples
  - Transactions and migrations
  - Backup and recovery

### Operations & Support

- **[TROUBLESHOOTING.md](./TROUBLESHOOTING.md)** - Common issues and solutions
  - Startup issues
  - Database connection problems
  - API errors
  - WebSocket issues
  - Performance troubleshooting

---

## 🤝 Contributing

- **[CONTRIBUTING.md](../CONTRIBUTING.md)** - Contribution guidelines
  - Development workflow
  - Code standards
  - Pull request process
  - Testing requirements
  - Commit message format

---

## 📋 Quick Reference

### Project Structure

```
Chat-Api/
├── docs/                      # Documentation
│   ├── INDEX.md              # This file
│   ├── ARCHITECTURE.md        # System design
│   ├── API.md                # REST/WebSocket API
│   ├── SETUP.md              # Setup guide
│   ├── DEVELOPMENT.md        # Development guide
│   ├── DATABASE.md           # Database schema
│   └── TROUBLESHOOTING.md    # Troubleshooting
├── .github/                   # GitHub workflows & templates
│   ├── workflows/            # CI/CD pipelines
│   └── ISSUE_TEMPLATE/       # Issue templates
├── apiContact/               # Main application
│   ├── Controllers/          # API endpoints
│   ├── Features/             # CQRS commands/queries
│   ├── Hubs/                # SignalR WebSocket hubs
│   ├── Services/            # Business logic
│   ├── Models/              # Data models & DTOs
│   ├── Data/                # Database layer
│   ├── Middleware/          # Custom middleware
│   ├── Mappings/            # AutoMapper profiles
│   └── Program.cs           # Application startup
├── README.md                # Project overview
├── CONTRIBUTING.md          # Contributing guidelines
└── LICENSE.txt              # MIT License
```

### Technology Stack

| Component | Technology |
|-----------|-----------|
| Framework | ASP.NET Core 9 |
| Database | MongoDB |
| Caching | Redis (optional) |
| Real-time | SignalR |
| Auth | JWT + BCrypt |
| API Docs | Swagger/OpenAPI |

---

## 🎯 Common Tasks

### I want to...

#### ...get the API running locally

1. Read [SETUP.md](./SETUP.md) - Follow the local development setup
2. Check [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) if issues arise
3. Access Swagger UI at `http://localhost:5000/swagger`

#### ...understand the architecture

1. Start with [ARCHITECTURE.md](./ARCHITECTURE.md)
2. Review [DATABASE.md](./DATABASE.md) for data models
3. Check [DEVELOPMENT.md](./DEVELOPMENT.md) for code organization

#### ...add a new feature

1. Read [DEVELOPMENT.md](./DEVELOPMENT.md) - "Adding a New Feature" section
2. Follow the CQRS pattern in Features/
3. Add tests (see Development Guide)
4. Update API docs if endpoints change

#### ...fix a bug

1. Reproduce with a test case
2. Check [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) for similar issues
3. Fix the bug with a test covering it
4. Submit a PR with your changes

#### ...deploy to production

1. Read [SETUP.md](./SETUP.md) - "Production Deployment" section
2. Set up MongoDB Atlas or managed instance
3. Configure environment variables
4. Use Docker for containerized deployment

#### ...contribute to the project

1. Read [CONTRIBUTING.md](../CONTRIBUTING.md)
2. Follow the development workflow
3. Ensure tests pass and code is formatted
4. Submit a pull request

---

## 🔗 External Resources

### Official Documentation

- [ASP.NET Core Docs](https://docs.microsoft.com/en-us/aspnet/core/)
- [MongoDB C# Driver](https://docs.mongodb.com/drivers/csharp/)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [MediatR GitHub](https://github.com/jbogard/MediatR)

### Learning Resources

- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

## 📞 Getting Help

### Documentation Issues

- Can't find something? Check the [Index](#) above
- Confusing explanation? Check the related section
- Found an error? Suggest an edit in the repo

### Technical Issues

- 🐛 Report bugs: [GitHub Issues](https://github.com/Mostafa-SAID7/Chat-Api/issues)
- 💬 Ask questions: [GitHub Discussions](https://github.com/Mostafa-SAID7/Chat-Api/discussions)
- 🤝 Contribute: [CONTRIBUTING.md](../CONTRIBUTING.md)

---

## 📝 Documentation Map

```
Quick Start
    ↓
README.md ─→ SETUP.md ─→ Running Locally
    ↓           ↓
Features    Architecture
    ↓           ↓
API.md  ←─→ DATABASE.md
    ↓           ↓
Development Guide
    ↓
Testing & Code Standards
    ↓
Contributing
    ↓
Submit PR
```

---

## 📋 Checklist for New Contributors

- [ ] Read README.md
- [ ] Complete SETUP.md locally
- [ ] Review ARCHITECTURE.md
- [ ] Read CONTRIBUTING.md
- [ ] Read relevant docs for your task
- [ ] Follow code standards in DEVELOPMENT.md
- [ ] Write tests
- [ ] Submit PR

---

## 📅 Last Updated

- **Documentation Version**: 1.0
- **Last Update**: June 2026
- **Maintained By**: Mostafa Said & Contributors

---

<div align="center">

[📖 View on GitHub](https://github.com/Mostafa-SAID7/Chat-Api/tree/main/docs) | [🐛 Report Issue](https://github.com/Mostafa-SAID7/Chat-Api/issues) | [💬 Discuss](https://github.com/Mostafa-SAID7/Chat-Api/discussions)

**Made with ❤️ for developers**

</div>
