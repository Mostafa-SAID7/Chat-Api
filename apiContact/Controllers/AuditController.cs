using apiContact.Data.Repositories;
using apiContact.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace apiContact.Controllers
{
    /// <summary>Admin-only — read the security audit trail</summary>
    [ApiController]
    [Route("api/audit")]
    [Authorize(Roles = "Admin")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditRepository               _audit;
        private readonly ILogger<AuditController>       _log;

        public AuditController(IAuditRepository audit, ILogger<AuditController> log)
        {
            _audit = audit;
            _log   = log;
        }

        /// <summary>Most recent audit events (max 500)</summary>
        [HttpGet]
        public async Task<IActionResult> GetRecent([FromQuery] int limit = 100)
        {
            limit = Math.Clamp(limit, 1, 500);
            var entries = await _audit.GetRecentAsync(limit);
            return Ok(ApiResponse<object>.Ok(entries, $"{entries.Count} audit entries"));
        }

        /// <summary>Audit events filtered by user, action prefix, and/or date range</summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string?   userId = null,
            [FromQuery] string?   action = null,
            [FromQuery] DateTime? from   = null,
            [FromQuery] DateTime? to     = null,
            [FromQuery] int       limit  = 100)
        {
            limit = Math.Clamp(limit, 1, 500);
            var entries = await _audit.SearchAsync(userId, action, from, to, limit);
            return Ok(ApiResponse<object>.Ok(entries, $"{entries.Count} matching entries"));
        }

        /// <summary>All audit events for a single user</summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> ByUser(string userId, [FromQuery] int limit = 50)
        {
            limit = Math.Clamp(limit, 1, 200);
            var entries = await _audit.GetByUserAsync(userId, limit);
            return Ok(ApiResponse<object>.Ok(entries, $"{entries.Count} entries for user {userId}"));
        }

        /// <summary>All audit events matching an action prefix (e.g. "auth", "file.delete")</summary>
        [HttpGet("action/{action}")]
        public async Task<IActionResult> ByAction(string action, [FromQuery] int limit = 50)
        {
            limit = Math.Clamp(limit, 1, 200);
            var entries = await _audit.GetByActionAsync(action, limit);
            return Ok(ApiResponse<object>.Ok(entries, $"{entries.Count} entries for action {action}"));
        }
    }
}
