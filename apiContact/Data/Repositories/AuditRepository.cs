using apiContact.Models.Entities;
using MongoDB.Driver;

namespace apiContact.Data.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly ChatDbContext              _db;
        private readonly IMongoCollection<AuditLog>? _col;

        public AuditRepository(ChatDbContext db)
        {
            _db  = db;
            _col = db.GetCollection<AuditLog>("audit_logs");
        }

        public async Task AddAsync(AuditLog entry)
        {
            if (_db.IsInMemory)
            {
                _db.AuditLogs[entry.Id] = entry;
                return;
            }
            await _col!.InsertOneAsync(entry);
        }

        public async Task<List<AuditLog>> GetRecentAsync(int limit = 100)
        {
            if (_db.IsInMemory)
                return _db.AuditLogs.Values
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToList();

            return await _col!.Find(_ => true)
                .SortByDescending(a => a.Timestamp)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetByUserAsync(string userId, int limit = 50)
        {
            if (_db.IsInMemory)
                return _db.AuditLogs.Values
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToList();

            return await _col!.Find(a => a.UserId == userId)
                .SortByDescending(a => a.Timestamp)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetByActionAsync(string action, int limit = 50)
        {
            if (_db.IsInMemory)
                return _db.AuditLogs.Values
                    .Where(a => a.Action == action)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .ToList();

            return await _col!.Find(a => a.Action == action)
                .SortByDescending(a => a.Timestamp)
                .Limit(limit)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> SearchAsync(
            string? userId, string? action, DateTime? from, DateTime? to, int limit = 100)
        {
            if (_db.IsInMemory)
            {
                var q = _db.AuditLogs.Values.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(userId)) q = q.Where(a => a.UserId == userId);
                if (!string.IsNullOrWhiteSpace(action)) q = q.Where(a => a.Action.StartsWith(action, StringComparison.OrdinalIgnoreCase));
                if (from.HasValue)  q = q.Where(a => a.Timestamp >= from.Value);
                if (to.HasValue)    q = q.Where(a => a.Timestamp <= to.Value);
                return q.OrderByDescending(a => a.Timestamp).Take(limit).ToList();
            }

            var b       = Builders<AuditLog>.Filter;
            var filters = new List<FilterDefinition<AuditLog>>();

            if (!string.IsNullOrWhiteSpace(userId)) filters.Add(b.Eq(a => a.UserId, userId));
            if (!string.IsNullOrWhiteSpace(action)) filters.Add(b.Regex(a => a.Action, new MongoDB.Bson.BsonRegularExpression($"^{action}", "i")));
            if (from.HasValue) filters.Add(b.Gte(a => a.Timestamp, from.Value));
            if (to.HasValue)   filters.Add(b.Lte(a => a.Timestamp, to.Value));

            var filter = filters.Count > 0 ? b.And(filters) : b.Empty;

            return await _col!.Find(filter)
                .SortByDescending(a => a.Timestamp)
                .Limit(limit)
                .ToListAsync();
        }
    }
}
