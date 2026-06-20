using System.Net;
using System.Text.Json;
using apiContact.Models.Dtos;

namespace apiContact.Middleware
{
    /// <summary>
    /// Catches any unhandled exception, logs it with full structured context,
    /// and returns a safe generic 500 JSON response — never leaking stack traces
    /// or internal details to the caller.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate                      _next;
        private readonly ILogger<GlobalExceptionMiddleware>   _log;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public GlobalExceptionMiddleware(
            RequestDelegate                    next,
            ILogger<GlobalExceptionMiddleware> log)
        {
            _next = next;
            _log  = log;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (Exception ex)
            {
                var ip     = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var method = ctx.Request.Method;
                var path   = ctx.Request.Path;
                var user   = ctx.User.Identity?.Name ?? "anonymous";

                _log.LogError(ex,
                    "Unhandled exception | {Method} {Path} | user={User} ip={Ip} | {Message}",
                    method, path, user, ip, ex.Message);

                if (ctx.Response.HasStarted) return;

                ctx.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
                ctx.Response.ContentType = "application/json";

                var body = JsonSerializer.Serialize(
                    ApiResponse<object>.Fail("An unexpected error occurred. Please try again later."),
                    _json);

                await ctx.Response.WriteAsync(body);
            }
        }
    }

    public static class GlobalExceptionExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
            => app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
