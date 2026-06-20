using apiContact.Data;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace apiContact.Services
{
    public class UserService : IUserService
    {
        private readonly ChatDbContext _db;
        private readonly IMongoCollection<ChatUser>? _col;

        public UserService(ChatDbContext db)
        {
            _db = db;
            _col = db.GetCollection<ChatUser>("users");
        }

        public async Task<List<ChatUser>> GetAllAsync()
        {
            if (_db.IsInMemory) return _db.Users.Values.OrderBy(u => u.DisplayName).ToList();
            return await _col!.Find(_ => true).SortBy(u => u.DisplayName).ToListAsync();
        }

        public async Task<ChatUser?> GetByIdAsync(string id)
        {
            if (_db.IsInMemory) return _db.Users.GetValueOrDefault(id);
            return await _col!.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<ChatUser?> GetByUsernameAsync(string username)
        {
            if (_db.IsInMemory)
                return _db.Users.Values.FirstOrDefault(
                    u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            return await _col!.Find(u => u.Username == username).FirstOrDefaultAsync();
        }

        public async Task<ChatUser?> GetByEmailAsync(string email)
        {
            if (_db.IsInMemory)
                return _db.Users.Values.FirstOrDefault(
                    u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return await _col!.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

        public async Task<ChatUser> CreateAsync(CreateUserDto dto)
        {
            var user = new ChatUser
            {
                Id           = ObjectId.GenerateNewId().ToString(),
                Username     = dto.Username.Trim().ToLower(),
                DisplayName  = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.Username : dto.DisplayName,
                Email        = dto.Email.Trim().ToLower(),
                AvatarUrl    = dto.AvatarUrl,
                Role         = dto.Role,
                PasswordHash = dto.Password,   // caller must pass an already-hashed value
                CreatedAt    = DateTime.UtcNow
            };
            if (_db.IsInMemory) { _db.Users[user.Id] = user; return user; }
            await _col!.InsertOneAsync(user);
            return user;
        }

        public async Task<ChatUser?> UpdateAsync(string id, UpdateUserDto dto)
        {
            var user = await GetByIdAsync(id);
            if (user == null) return null;
            if (dto.DisplayName != null) user.DisplayName = dto.DisplayName;
            if (dto.AvatarUrl   != null) user.AvatarUrl   = dto.AvatarUrl;
            if (dto.IsOnline.HasValue)
            {
                user.IsOnline = dto.IsOnline.Value;
                user.LastSeen = DateTime.UtcNow;
            }
            if (_db.IsInMemory) { _db.Users[id] = user; return user; }
            await _col!.ReplaceOneAsync(u => u.Id == id, user);
            return user;
        }

        public async Task<bool> ChangePasswordAsync(string id, string newPasswordHash)
        {
            var user = await GetByIdAsync(id);
            if (user == null) return false;
            user.PasswordHash = newPasswordHash;
            if (_db.IsInMemory) { _db.Users[id] = user; return true; }
            await _col!.UpdateOneAsync(u => u.Id == id,
                Builders<ChatUser>.Update.Set(u => u.PasswordHash, newPasswordHash));
            return true;
        }

        public async Task SaveRefreshTokenAsync(string id, string? token, DateTime? expiry)
        {
            var user = await GetByIdAsync(id);
            if (user == null) return;
            user.RefreshToken       = token;
            user.RefreshTokenExpiry = expiry;
            if (_db.IsInMemory) { _db.Users[id] = user; return; }
            await _col!.UpdateOneAsync(u => u.Id == id,
                Builders<ChatUser>.Update
                    .Set(u => u.RefreshToken,       token)
                    .Set(u => u.RefreshTokenExpiry, expiry));
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (_db.IsInMemory) return _db.Users.Remove(id);
            var result = await _col!.DeleteOneAsync(u => u.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task SetOnlineAsync(string id, bool isOnline)
            => await UpdateAsync(id, new UpdateUserDto { IsOnline = isOnline });
    }
}
