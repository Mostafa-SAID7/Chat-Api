namespace apiContact.Services
{
    /// <summary>
    /// Records security-relevant events with structured fields for audit trails.
    /// Every call is fire-and-keep — it never throws to the caller.
    /// </summary>
    public interface IAuditService
    {
        /// <param name="action">Dot-notated action key, e.g. "auth.login", "room.archive"</param>
        /// <param name="userId">Authenticated user id (null for anonymous)</param>
        /// <param name="username">Display username for readability</param>
        /// <param name="ipAddress">Caller IP address</param>
        /// <param name="success">Whether the action was permitted / completed</param>
        /// <param name="resourceId">Id of the primary resource affected</param>
        /// <param name="resourceType">Type label of that resource</param>
        /// <param name="details">Extra context (no PII, no secrets)</param>
        Task LogAsync(
            string  action,
            string? userId,
            string? username,
            string? ipAddress,
            bool    success      = true,
            string? resourceId   = null,
            string? resourceType = null,
            string? details      = null);
    }
}
