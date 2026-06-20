# 🚀 Running Chat API on Replit

This guide helps you run Chat API on Replit for development and testing.

## Quick Start (30 seconds)

1. **Click "Run"** in Replit
2. Wait for build to complete (2-3 minutes)
3. Access the API from the provided URL
4. Open Swagger UI: `{replit-url}/swagger`

## What's Included

- ✅ ASP.NET Core 9 runtime
- ✅ .NET SDK
- ✅ In-memory MongoDB (no external DB needed)
- ✅ Development configuration
- ✅ Auto-reload on code changes
- ✅ Swagger UI for API testing

## Access Points

| Service | URL | Notes |
|---------|-----|-------|
| **API** | `{replit-url}` | Main API endpoint |
| **Swagger UI** | `{replit-url}/swagger` | Interactive API docs |
| **Health Check** | `{replit-url}/health` | Service health status |

## Development

### Make Changes

1. Edit files directly in Replit
2. Changes auto-reload on save
3. Check logs for errors

### Available Commands

```bash
# Build project
dotnet build

# Run tests
dotnet test

# Clean build
dotnet clean && dotnet build

# View logs
# Check the Console tab in Replit
```

## Environment Variables

Default development values are set in `.replit`:

```
JWT_KEY = your_super_secret_key_minimum_32_characters_long_for_development
ASPNETCORE_ENVIRONMENT = Development
MongoDB__ConnectionString = (In-memory by default)
```

To change, edit `.replit` file.

## Testing the API

### Option 1: Swagger UI (Easy)

1. Open `{replit-url}/swagger`
2. Try endpoints directly in browser

### Option 2: cURL

```bash
# Register user
curl -X POST https://{replit-url}/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email":"test@example.com",
    "password":"TestPass123",
    "firstName":"Test",
    "lastName":"User"
  }'

# Login
curl -X POST https://{replit-url}/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email":"test@example.com",
    "password":"TestPass123"
  }'
```

### Option 3: WebSocket (Real-time Chat)

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://{replit-url}/hubs/chat")
  .withAutomaticReconnect()
  .build();

connection.start();

connection.on("ReceiveMessage", (message) => {
  console.log("Message:", message);
});

connection.invoke("SendMessage", {
  receiverId: "user-id",
  text: "Hello!"
});
```

## Troubleshooting

### Build Fails

1. Check .replit file syntax
2. Ensure `appsettings.Development.json` exists
3. Try: `dotnet clean && dotnet restore`

### API Not Responding

1. Check console for errors
2. Verify JWT_KEY is set (min 32 chars)
3. Try restarting with Run button

### Port Already in Use

Replit auto-selects available port. Check URL in browser.

## Performance Tips

- ✅ First run takes 2-3 minutes (downloading dependencies)
- ✅ Subsequent runs are much faster
- ✅ Code changes reload automatically in development
- ✅ Keep Replit tab active for uninterrupted service

## Limitations

- ⚠️ In-memory database (data lost on restart)
- ⚠️ No persistent storage
- ⚠️ Single instance only
- ⚠️ Limited compute resources

## Deployment from Replit

When ready for production:

1. Use Docker: `docker-compose -f docker-compose.prod.yml up`
2. Deploy to cloud: Azure, AWS, DigitalOcean, etc.
3. See [SETUP.md](./docs/SETUP.md) for production guide

## File Structure in Replit

```
Chat-Api/
├── .replit                    (Replit config)
├── .replit-setup.sh          (Setup script)
├── REPLIT_README.md          (This file)
├── README.md
├── docs/                     (Documentation)
├── apiContact/               (Main project)
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Program.cs
│   └── ...
└── ...
```

## Getting Help

- 📖 Full docs: See `/docs` folder
- 🐛 Report bugs: GitHub Issues
- 💬 Questions: GitHub Discussions
- 📚 API docs: `{replit-url}/swagger`

## Next Steps

1. ✅ API is running
2. 📖 Read [API.md](./docs/API.md) to understand endpoints
3. 💻 Explore [DEVELOPMENT.md](./docs/DEVELOPMENT.md) for code standards
4. 🤝 Check [CONTRIBUTING.md](./CONTRIBUTING.md) to contribute

---

**Happy coding! 🎉**

[View Main Documentation](./README.md) | [API Docs](./docs/API.md) | [Architecture](./docs/ARCHITECTURE.md)
