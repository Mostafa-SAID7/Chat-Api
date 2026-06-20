using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using apiContact.Models.Entities;
using Microsoft.IdentityModel.Tokens;

namespace apiContact.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly string         _jwtKey;

        public AuthService(IConfiguration config)
        {
            _config = config;
            // Prefer env var (production secret) over appsettings fallback
            _jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
                   ?? config["Jwt:Key"]
                   ?? throw new InvalidOperationException(
                          "JWT key is not configured. Set the JWT_KEY environment variable.");
        }

        public string GenerateAccessToken(ChatUser user)
        {
            var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,        user.Id),
                new Claim(JwtRegisteredClaimNames.Email,      user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim("displayName",                      user.DisplayName),
                new Claim(ClaimTypes.Role,                    user.Role),
                new Claim(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString())
            };

            var expiryMinutes = int.TryParse(
                _config["Jwt:AccessTokenExpiryMinutes"], out var m) ? m : 60;

            var token = new JwtSecurityToken(
                issuer:             _config["Jwt:Issuer"],
                audience:           _config["Jwt:Audience"],
                claims:             claims,
                expires:            DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        public string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        public bool VerifyPassword(string password, string hash)
            => BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
