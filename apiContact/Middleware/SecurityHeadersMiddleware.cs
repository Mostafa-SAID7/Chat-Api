namespace apiContact.Middleware
{
    /// <summary>
    /// Adds hardened HTTP security headers to every response.
    /// Place this early in the pipeline (before static files) so even
    /// static assets carry the headers.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var h = context.Response.Headers;

            // Prevent MIME-type sniffing
            h["X-Content-Type-Options"] = "nosniff";

            // Block framing — prevents clickjacking
            h["X-Frame-Options"] = "DENY";

            // Legacy XSS filter (belt-and-suspenders for older browsers)
            h["X-XSS-Protection"] = "1; mode=block";

            // Control referrer information leak
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Restrict browser feature APIs
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

            // Content Security Policy
            // - 'unsafe-inline' on script/style is intentional for the static frontend
            //   (Lucide + inline event handlers). Tighten with nonces in the future.
            h["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' https://unpkg.com; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com; " +
                "img-src 'self' data: blob: https:; " +
                "connect-src 'self' wss: ws:; " +
                "frame-ancestors 'none';";

            // HSTS — only meaningful over HTTPS; skip on plaintext dev connections
            if (context.Request.IsHttps)
                h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";

            // Remove server identification banner
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await _next(context);
        }
    }

    public static class SecurityHeadersExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
            => app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
