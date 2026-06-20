using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace apiContact.Models.Entities
{
    /// <summary>
    /// Persistent record of every security-relevant action in the system.
    /// Written by AuditService; never modified after creation.
    /// </summary>
    public class AuditLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        /// <summary>Machine-readable action key, e.g. "auth.login", "message.delete"</summary>
        public string  Action       { get; set; } = string.Empty;

        public string? UserId       { get; set; }
        public string? Username     { get; set; }

        /// <summary>Primary resource affected (roomId, messageId, fileName …)</summary>
        public string? ResourceId   { get; set; }

        /// <summary>Resource type, e.g. "Room", "Message", "File"</summary>
        public string? ResourceType { get; set; }

        public string? IpAddress    { get; set; }
        public string? UserAgent    { get; set; }

        /// <summary>True when the action completed successfully; false on denied/failed</summary>
        public bool    Success      { get; set; } = true;

        /// <summary>Optional free-form context (sanitised — no PII, no secrets)</summary>
        public string? Details      { get; set; }

        public DateTime Timestamp   { get; set; } = DateTime.UtcNow;
    }
}
