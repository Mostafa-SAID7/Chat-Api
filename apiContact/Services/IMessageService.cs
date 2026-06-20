using apiContact.Models.Dtos;
using apiContact.Models.Entities;

namespace apiContact.Services
{
    public interface IMessageService
    {
        Task<List<Message>> GetByRoomAsync(string roomId, int limit = 50, int skip = 0);
        Task<Message?> GetByIdAsync(string id);
        Task<Message> SendAsync(SendMessageDto dto, string senderName);
        Task<Message?> EditAsync(string id, string senderId, string content);
        Task<bool> DeleteAsync(string id, string senderId);
        Task MarkReadAsync(string id, string userId);
        Task<int> GetUnreadCountAsync(string roomId, string userId);
    }
}
