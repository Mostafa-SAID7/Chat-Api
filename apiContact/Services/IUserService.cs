using apiContact.Models.Dtos;
using apiContact.Models.Entities;

namespace apiContact.Services
{
    public interface IUserService
    {
        Task<List<ChatUser>> GetAllAsync();
        Task<ChatUser?> GetByIdAsync(string id);
        Task<ChatUser?> GetByUsernameAsync(string username);
        Task<ChatUser?> GetByEmailAsync(string email);
        Task<ChatUser> CreateAsync(CreateUserDto dto);
        Task<ChatUser?> UpdateAsync(string id, UpdateUserDto dto);
        Task<bool> ChangePasswordAsync(string id, string newPasswordHash);
        Task SaveRefreshTokenAsync(string id, string? token, DateTime? expiry);
        Task<bool> DeleteAsync(string id);
        Task SetOnlineAsync(string id, bool isOnline);
    }
}
