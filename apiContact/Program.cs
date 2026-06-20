using System.Text;
using apiContact.Data;
using apiContact.Hubs;
using apiContact.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers & API explorer ────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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

    // Bearer token input in Swagger UI
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
var jwtKey = builder.Configuration["Jwt:Key"]!;
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

        // Allow SignalR to read token from query string
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

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddSingleton<ChatDbContext>();
builder.Services.AddScoped<IAuthService,    AuthService>();
builder.Services.AddScoped<IUserService,    UserService>();
builder.Services.AddScoped<IRoomService,    RoomService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IFileService,    FileService>();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("ChatPolicy", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.WebHost.UseUrls("http://0.0.0.0:5000");

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat API v1");
    c.RoutePrefix    = "swagger";
    c.DocumentTitle  = "Chat API — Swagger UI";
    c.InjectStylesheet("/css/swagger-custom.css");
});

app.UseCors("ChatPolicy");
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();   // must come before UseAuthorization
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.MapGet("/health", () => new
{
    status    = "healthy",
    timestamp = DateTime.UtcNow,
    version   = "1.0.0",
    services  = new
    {
        auth      = "JWT Bearer",
        websocket = "SignalR",
        database  = "MongoDB (in-memory fallback)",
        cache     = "Redis (optional)",
        storage   = "Blob (local)"
    }
});

app.Run();
