using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using apiContact.Data;
using apiContact.Data.Repositories;
using apiContact.Hubs;
using apiContact.Middleware;
using apiContact.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers & API explorer ────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── MediatR (CQRS) ────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// ── Swagger with JWT Bearer support ──────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Chat API",
        Version     = "v1",
        Description = "Realtime chat API · WebSocket (SignalR) · MongoDB · Redis · Blob Storage",
        Contact     = new OpenApiContact { Name = "Chat API", Email = "api@chat.io" }
    });

    c.EnableAnnotations();

    var scheme = new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT access token. Example: `eyJhbGci...`"
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
          ?? builder.Configuration["Jwt:Key"]
          ?? throw new InvalidOperationException(
                 "JWT key is not configured. Set the JWT_KEY environment variable.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.Zero
        };

        // Allow SignalR to read JWT from query string (WebSocket handshake cannot set headers)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                var path  = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── IMemoryCache (L1 cache — always available) ────────────────────────────────
builder.Services.AddMemoryCache();

// ── Redis (L2 cache — optional; skipped gracefully when not configured) ───────
var redisConnStr = builder.Configuration["Redis:ConnectionString"]
               ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

// Fix LangCache Redis format: redis://:password@host -> host:password
if (!string.IsNullOrWhiteSpace(redisConnStr) && redisConnStr.StartsWith("redis://"))
{
    // Convert redis://:password@host:port to host:port,password=password format
    try
    {
        var uri = new Uri(redisConnStr);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 6379;
        var password = uri.UserInfo?.Split(':').LastOrDefault();
        
        if (!string.IsNullOrWhiteSpace(password))
        {
            redisConnStr = $"{host}:{port},password={password}";
        }
        else
        {
            redisConnStr = $"{host}:{port}";
        }
    }
    catch
    {
        // If parsing fails, try as-is
    }
}

var redisAvailable = false;
if (!string.IsNullOrWhiteSpace(redisConnStr))
{
    try
    {
        var redis = await ConnectionMultiplexer.ConnectAsync(redisConnStr);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
        redisAvailable = true;
    }
    catch (Exception ex)
    {
        var startupLog = LoggerFactory.Create(lb => lb.AddConsole()).CreateLogger("Startup");
        startupLog.LogWarning(ex, "Redis connection failed — continuing with in-memory cache only");
        // Register null for Redis when connection fails
        builder.Services.AddSingleton(sp => (IConnectionMultiplexer?)null);
    }
}
else
{
    // Register null for Redis when not configured
    builder.Services.AddSingleton(sp => (IConnectionMultiplexer?)null);
}

// ── Cache service (wraps Redis + IMemoryCache with transparent fallback) ──────
builder.Services.AddSingleton<ICacheService, CacheService>();

// ── Rate Limiting ─────────────────────────────────────────────────────────────
// Fixed-window limiter on auth and file endpoints to resist brute-force / abuse.
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Auth endpoints: 10 requests / IP / minute
    opt.AddFixedWindowLimiter("auth", limiterOpts =>
    {
        limiterOpts.Window            = TimeSpan.FromMinutes(1);
        limiterOpts.PermitLimit       = 10;
        limiterOpts.QueueLimit        = 0;
        limiterOpts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // File upload: 20 requests / IP / minute
    opt.AddFixedWindowLimiter("files", limiterOpts =>
    {
        limiterOpts.Window            = TimeSpan.FromMinutes(1);
        limiterOpts.PermitLimit       = 20;
        limiterOpts.QueueLimit        = 0;
        limiterOpts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Global API limiter: 120 requests / IP / minute for everything else
    opt.AddFixedWindowLimiter("global", limiterOpts =>
    {
        limiterOpts.Window            = TimeSpan.FromMinutes(1);
        limiterOpts.PermitLimit       = 120;
        limiterOpts.QueueLimit        = 0;
        limiterOpts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// ── MongoDB Client & Database ───────────────────────────────────────────────
var mongoConnStr = builder.Configuration["MongoDB:ConnectionString"]
                ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
var mongoDbName  = builder.Configuration["MongoDB:DatabaseName"] 
                ?? Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
                ?? "ChatDb";

IMongoClient? registeredMongoClient = null;
IMongoDatabase? registeredMongoDb = null;

if (!string.IsNullOrWhiteSpace(mongoConnStr))
{
    try
    {
        registeredMongoClient = new MongoClient(mongoConnStr);
        registeredMongoDb = registeredMongoClient.GetDatabase(mongoDbName);
        builder.Services.AddSingleton(registeredMongoClient);
        builder.Services.AddSingleton(registeredMongoDb);
    }
    catch (Exception ex)
    {
        var logger = LoggerFactory.Create(lb => lb.AddConsole()).CreateLogger("Startup");
        logger.LogWarning(ex, "MongoDB connection failed - using in-memory fallback");
    }
}

// Ensure services are available even if MongoDB fails
if (registeredMongoClient == null)
{
    builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient());
}
if (registeredMongoDb == null)
{
    builder.Services.AddSingleton<IMongoDatabase>(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDbName));
}

// ── Repository layer (Unit of Work + Repositories) ───────────────────────────
builder.Services.AddScoped<IUserRepository,    UserRepository>();
builder.Services.AddScoped<IRoomRepository,    RoomRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IAuditRepository,   AuditRepository>();
builder.Services.AddScoped<IUnitOfWork,        UnitOfWork>();

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddSingleton<ChatDbContext>();
builder.Services.AddScoped<IAuthService,    AuthService>();
builder.Services.AddScoped<IUserService,    UserService>();
builder.Services.AddScoped<IRoomService,    RoomService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IFileService,    FileService>();
builder.Services.AddScoped<IAuditService,   AuditService>();

// ── Database Migration & Connection Verification ────────────────────────────
builder.Services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
builder.Services.AddScoped<IRedisConnectionService, RedisConnectionService>();
builder.Services.AddScoped<IDatabaseSeedService, DatabaseSeedService>();

// ── CORS ──────────────────────────────────────────────────────────────────────
var isDev = builder.Environment.IsDevelopment();

var replitDev    = Environment.GetEnvironmentVariable("REPLIT_DEV_DOMAIN") ?? "";
var replitDomain = Environment.GetEnvironmentVariable("REPLIT_DOMAINS")    ?? "";

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());

    var allowedOrigins = new List<string>
    {
        "https://*.replit.app",
        "https://*.replit.dev",
        "https://*.repl.co"
    };
    if (!string.IsNullOrWhiteSpace(replitDev))    allowedOrigins.Add($"https://{replitDev}");
    if (!string.IsNullOrWhiteSpace(replitDomain)) allowedOrigins.Add($"https://{replitDomain}");

    options.AddPolicy("ProductionPolicy", policy =>
        policy.SetIsOriginAllowedToAllowWildcardSubdomains()
              .WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.WebHost.UseUrls("http://0.0.0.0:5000");

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
// 1. Global exception handler — outermost so it catches everything
app.UseGlobalExceptionHandler();

// 2. Security headers — applied to every response including static files
app.UseSecurityHeaders();

// 3. Rate limiting
app.UseRateLimiter();

// 4. Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat API v1");
    c.RoutePrefix    = "swagger";
    c.DocumentTitle  = "Chat API — Swagger UI";
    c.InjectStylesheet("/css/swagger-custom.css");
    c.InjectJavascript("/js/swagger-nav.js");
});

// 5. CORS
app.UseCors(isDev ? "DevelopmentPolicy" : "ProductionPolicy");

// 6. Custom 404 handler — redirect unknown browser routes to /404.html
//    API/hub/swagger 404s stay as JSON (HasStarted = true by then)
app.Use(async (ctx, next) =>
{
    await next();
    var p = ctx.Request.Path.Value ?? "";
    if (ctx.Response.StatusCode == 404
        && !p.StartsWith("/api",     StringComparison.OrdinalIgnoreCase)
        && !p.StartsWith("/hubs",    StringComparison.OrdinalIgnoreCase)
        && !p.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
        && !p.StartsWith("/health",  StringComparison.OrdinalIgnoreCase)
        && !ctx.Response.HasStarted)
    {
        ctx.Response.Redirect("/404.html");
    }
});

// 7. Static files
app.UseDefaultFiles();
app.UseStaticFiles();

// 8. Auth
app.UseAuthentication();
app.UseAuthorization();

// 9. Endpoints
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

// ── Health check ──────────────────────────────────────────────────────────────
app.MapGet("/health", () => new
{
    status    = "healthy",
    timestamp = DateTime.UtcNow,
    version   = "1.0.0",
    services  = new
    {
        auth      = "JWT Bearer (HS256, key from JWT_KEY env var)",
        websocket = "SignalR (authenticated)",
        database  = "MongoDB (in-memory fallback)",
        cache     = redisAvailable ? "Redis (connected) + IMemoryCache L1" : "IMemoryCache (Redis not configured)",
        storage   = "Blob (local wwwroot/uploads)",
        audit     = "AuditService → AuditRepository (persistent)",
        rateLimit = "Fixed-window: auth=10/min, files=20/min, global=120/min",
        pattern   = "Repository + Unit of Work + MediatR CQRS"
    }
});

// ── Initialize Database & Run Migrations ──────────────────────────────────────
try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("🔄 Starting database initialization...");
    
    using (var scope = app.Services.CreateScope())
    {
        // Verify MongoDB connection
        var mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        var mongoDb = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        
        logger.LogInformation("🔗 Verifying MongoDB connection...");
        var adminDb = mongoClient.GetDatabase("admin");
        var pingCommand = new MongoDB.Bson.BsonDocument("ping", 1);
        await adminDb.RunCommandAsync<MongoDB.Bson.BsonDocument>(pingCommand);
        logger.LogInformation("✅ MongoDB connection verified");
        
        // Run database migrations
        var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
        await migrationService.InitializeDatabaseAsync();
        logger.LogInformation("✅ Database initialization completed");
        
        // Seed initial data
        var seedService = scope.ServiceProvider.GetRequiredService<IDatabaseSeedService>();
        await seedService.SeedAsync();
        
        // Verify Redis connection
        var redisService = scope.ServiceProvider.GetRequiredService<IRedisConnectionService>();
        var isRedisHealthy = await redisService.VerifyConnectionAsync();
        if (isRedisHealthy)
        {
            logger.LogInformation("✅ Redis connection verified");
        }
        else
        {
            logger.LogWarning("⚠️ Redis connection failed - using in-memory cache");
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "❌ Error during database initialization. Some features may not work correctly.");
}

app.Run();
