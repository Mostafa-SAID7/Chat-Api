using apiContact.Models.Entities;

namespace apiContact.Services
{
    public interface IAuthService
    {
        string GenerateAccessToken(ChatUser user);
        string GenerateRefreshToken();
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }
}
