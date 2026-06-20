using apiContact.Data.Repositories;
using apiContact.Models.Entities;

namespace apiContact.Services
{
    /// <summary>
    /// Writes structured audit entries to the persistent audit log and simultaneously
    /// emits a structured log line so the entries appear in the application log stream.
    /// Errors in the audit pipeline are swallowed so they never surface to callers.
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository    _repo;
        private readonly ILogger<AuditService> _log;

        public AuditService(IAuditRepository repo, ILogger<AuditService> log)
        {
            _repo = repo;
            _log  = log;
        }

        public async Task LogAsync(
            string  action,
            string? userId,
            string? username,
            string? ipAddress,
            bool    success      = true,
            string? resourceId   = null,
            string? resourceType = null,
            string? details      = null)
        {
            try
            {
                var entry = new AuditLog
                {
                    Action       = action,
                    UserId       = userId,
                    Username     = username,
                    IpAddress    = ipAddress,
                    Success      = success,
                    ResourceId   = resourceId,
                    ResourceType = resourceType,
                    Details      = details,
                    Timestamp    = DateTime.UtcNow
                };

                // Structured log — picked up by any ILogger sink (console, Seq, Datadog, etc.)
                _log.LogInformation(
                    "[AUDIT] action={Action} user={Username}({UserId}) ip={Ip} " +
                    "success={Success} resource={ResourceType}:{ResourceId} details={Details}",
                    action,
                    username ?? "anonymous",
                    userId   ?? "-",
                    ipAddress ?? "unknown",
                    success,
                    resourceType ?? "-",
                    resourceId   ?? "-",
                    details ?? "");

                await _repo.AddAsync(entry);
            }
            catch (Exception ex)
            {
                // Never let audit failure crash the request
                _log.LogWarning(ex, "Audit write failed for action={Action}", action);
            }
        }
    }
}
