using System.Security.Claims;
using apiContact.Models.Dtos;
using apiContact.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace apiContact.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _users;
        private readonly IAuthService _auth;
        private readonly IConfiguration _config;

        public AuthController(IUserService users, IAuthService auth, IConfiguration config)
        {
            _users  = users;
            _auth   = auth;
            _config = config;
        }

        /// <summary>Register a new account</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(ApiResponse<object>.Fail("Username and password are required"));

            if (dto.Password.Length < 6)
                return BadRequest(ApiResponse<object>.Fail("Password must be at least 6 characters"));

            if (await _users.GetByUsernameAsync(dto.Username) != null)
                return Conflict(ApiResponse<object>.Fail("Username already taken"));

            if (!string.IsNullOrWhiteSpace(dto.Email) && await _users.GetByEmailAsync(dto.Email) != null)
                return Conflict(ApiResponse<object>.Fail("Email already registered"));

            var user = await _users.CreateAsync(new CreateUserDto
            {
                Username    = dto.Username,
                DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.Username : dto.DisplayName,
                Email       = dto.Email,
                AvatarUrl   = dto.AvatarUrl,
                Password    = _auth.HashPassword(dto.Password),
                Role        = "user"
            });

            var refreshToken  = _auth.GenerateRefreshToken();
            var refreshExpiry = DateTime.UtcNow.AddDays(
                int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7"));

            await _users.SaveRefreshTokenAsync(user.Id, refreshToken, refreshExpiry);

            var accessToken  = _auth.GenerateAccessToken(user);
            var expiryMinutes = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

            return CreatedAtAction(nameof(Me), null, ApiResponse<object>.Ok(new AuthResponseDto
            {
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
                UserId       = user.Id,
                Username     = user.Username,
                DisplayName  = user.DisplayName,
                Role         = user.Role,
                ExpiresAt    = DateTime.UtcNow.AddMinutes(expiryMinutes)
            }, "Account created"));
        }

        /// <summary>Login with username/email and password</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UsernameOrEmail) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(ApiResponse<object>.Fail("Credentials are required"));

            var user = await _users.GetByUsernameAsync(dto.UsernameOrEmail)
                    ?? await _users.GetByEmailAsync(dto.UsernameOrEmail);

            if (user == null || !_auth.VerifyPassword(dto.Password, user.PasswordHash))
                return Unauthorized(ApiResponse<object>.Fail("Invalid credentials"));

            var refreshToken  = _auth.GenerateRefreshToken();
            var refreshExpiry = DateTime.UtcNow.AddDays(
                int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7"));

            await _users.SaveRefreshTokenAsync(user.Id, refreshToken, refreshExpiry);
            await _users.SetOnlineAsync(user.Id, true);

            var accessToken   = _auth.GenerateAccessToken(user);
            var expiryMinutes = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

            return Ok(ApiResponse<object>.Ok(new AuthResponseDto
            {
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
                UserId       = user.Id,
                Username     = user.Username,
                DisplayName  = user.DisplayName,
                Role         = user.Role,
                ExpiresAt    = DateTime.UtcNow.AddMinutes(expiryMinutes)
            }, "Login successful"));
        }

        /// <summary>Refresh access token using a valid refresh token</summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest(ApiResponse<object>.Fail("Refresh token is required"));

            var users    = await _users.GetAllAsync();
            var user     = users.FirstOrDefault(u =>
                u.RefreshToken == dto.RefreshToken &&
                u.RefreshTokenExpiry > DateTime.UtcNow);

            if (user == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid or expired refresh token"));

            var newRefreshToken  = _auth.GenerateRefreshToken();
            var refreshExpiry    = DateTime.UtcNow.AddDays(
                int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7"));

            await _users.SaveRefreshTokenAsync(user.Id, newRefreshToken, refreshExpiry);

            var accessToken   = _auth.GenerateAccessToken(user);
            var expiryMinutes = int.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "60");

            return Ok(ApiResponse<object>.Ok(new AuthResponseDto
            {
                AccessToken  = accessToken,
                RefreshToken = newRefreshToken,
                UserId       = user.Id,
                Username     = user.Username,
                DisplayName  = user.DisplayName,
                Role         = user.Role,
                ExpiresAt    = DateTime.UtcNow.AddMinutes(expiryMinutes)
            }, "Token refreshed"));
        }

        /// <summary>Logout — revokes the refresh token</summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _users.SaveRefreshTokenAsync(userId, null, null);
                await _users.SetOnlineAsync(userId, false);
            }

            return Ok(ApiResponse<object>.Ok(new { }, "Logged out"));
        }

        /// <summary>Get the currently authenticated user's profile</summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(ApiResponse<object>.Fail("Invalid token"));

            var user = await _users.GetByIdAsync(userId);
            if (user == null) return NotFound(ApiResponse<object>.Fail("User not found"));

            return Ok(ApiResponse<object>.Ok(new
            {
                user.Id, user.Username, user.DisplayName,
                user.Email, user.AvatarUrl, user.Role,
                user.IsOnline, user.LastSeen, user.CreatedAt
            }));
        }

        /// <summary>Change password for the authenticated user</summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(ApiResponse<object>.Fail("Both current and new password are required"));

            if (dto.NewPassword.Length < 6)
                return BadRequest(ApiResponse<object>.Fail("New password must be at least 6 characters"));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");

            var user = await _users.GetByIdAsync(userId!);
            if (user == null) return NotFound(ApiResponse<object>.Fail("User not found"));

            if (!_auth.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                return Unauthorized(ApiResponse<object>.Fail("Current password is incorrect"));

            await _users.ChangePasswordAsync(user.Id, _auth.HashPassword(dto.NewPassword));

            // Revoke all refresh tokens after password change
            await _users.SaveRefreshTokenAsync(user.Id, null, null);

            return Ok(ApiResponse<object>.Ok(new { }, "Password changed. Please log in again."));
        }
    }
}
