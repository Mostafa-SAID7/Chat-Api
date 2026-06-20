using apiContact.Models.Entities;

namespace apiContact.Data.Repositories
{
    public interface IAuditRepository
    {
        Task                 AddAsync(AuditLog entry);
        Task<List<AuditLog>> GetRecentAsync(int limit = 100);
        Task<List<AuditLog>> GetByUserAsync(string userId, int limit = 50);
        Task<List<AuditLog>> GetByActionAsync(string action, int limit = 50);
        Task<List<AuditLog>> SearchAsync(string? userId, string? action, DateTime? from, DateTime? to, int limit = 100);
    }
}
