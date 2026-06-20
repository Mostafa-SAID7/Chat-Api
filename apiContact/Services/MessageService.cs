using apiContact.Data;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace apiContact.Services
{
    public class MessageService : IMessageService
    {
        private readonly ChatDbContext _db;
        private readonly IMongoCollection<Message>? _col;

        public MessageService(ChatDbContext db)
        {
            _db  = db;
            _col = db.GetCollection<Message>("messages");
        }

        public async Task<List<Message>> GetByRoomAsync(string roomId, int limit = 50, int skip = 0)
        {
            if (_db.IsInMemory)
                return _db.Messages.Values
                    .Where(m => m.RoomId == roomId && !m.IsDeleted)
                    .OrderByDescending(m => m.Timestamp)
                    .Skip(skip).Take(limit)
                    .OrderBy(m => m.Timestamp)
                    .ToList();

            return await _col!.Find(m => m.RoomId == roomId && !m.IsDeleted)
                .SortByDescending(m => m.Timestamp)
                .Skip(skip).Limit(limit)
                .SortBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<Message?> GetByIdAsync(string id)
        {
            if (_db.IsInMemory) return _db.Messages.GetValueOrDefault(id);
            return await _col!.Find(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Message> SendAsync(SendMessageDto dto, string senderName)
        {
            var msg = new Message
            {
                Id         = ObjectId.GenerateNewId().ToString(),
                RoomId     = dto.RoomId,
                SenderId   = dto.SenderId,
                SenderName = senderName,
                Content    = dto.Content,
                Type       = dto.Type,
                Timestamp  = DateTime.UtcNow
            };
            if (_db.IsInMemory) { _db.Messages[msg.Id] = msg; return msg; }
            await _col!.InsertOneAsync(msg);
            return msg;
        }

        public async Task<Message?> EditAsync(string id, string senderId, string content)
        {
            var msg = await GetByIdAsync(id);
            if (msg == null || msg.SenderId != senderId) return null;
            msg.Content  = content;
            msg.IsEdited = true;
            if (_db.IsInMemory) { _db.Messages[id] = msg; return msg; }
            await _col!.UpdateOneAsync(m => m.Id == id,
                Builders<Message>.Update
                    .Set(m => m.Content, content)
                    .Set(m => m.IsEdited, true));
            return msg;
        }

        public async Task<bool> DeleteAsync(string id, string senderId)
        {
            var msg = await GetByIdAsync(id);
            if (msg == null) return false;
            msg.IsDeleted = true;
            msg.Content   = "[Message deleted]";
            if (_db.IsInMemory) { _db.Messages[id] = msg; return true; }
            await _col!.UpdateOneAsync(m => m.Id == id,
                Builders<Message>.Update
                    .Set(m => m.IsDeleted, true)
                    .Set(m => m.Content, "[Message deleted]"));
            return true;
        }

        public async Task MarkReadAsync(string id, string userId)
        {
            var msg = await GetByIdAsync(id);
            if (msg == null || msg.ReadBy.Contains(userId)) return;
            msg.ReadBy.Add(userId);
            if (_db.IsInMemory) { _db.Messages[id] = msg; return; }
            await _col!.UpdateOneAsync(m => m.Id == id,
                Builders<Message>.Update.AddToSet(m => m.ReadBy, userId));
        }

        public async Task<int> GetUnreadCountAsync(string roomId, string userId)
        {
            if (_db.IsInMemory)
                return _db.Messages.Values.Count(m =>
                    m.RoomId == roomId && !m.IsDeleted &&
                    !m.ReadBy.Contains(userId) && m.SenderId != userId);

            return (int)await _col!.CountDocumentsAsync(m =>
                m.RoomId == roomId && !m.IsDeleted &&
                !m.ReadBy.Contains(userId) && m.SenderId != userId);
        }
    }
}
