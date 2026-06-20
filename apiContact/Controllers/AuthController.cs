using System.Security.Claims;
using apiContact.Data.Repositories;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using apiContact.Models.Enums;
using apiContact.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MongoDB.Bson;

namespace apiContact.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IUnitOfWork            _uow;
        private readonly IAuthService           _auth;
        private readonly IConfiguration         _config;
        private readonly IAuditService          _audit;
        private readonly ILogger<AuthController> _log;

        public AuthController(
            IUnitOfWork             uow,
            IAuthService            auth,
            IConfiguration          config,
            IAuditService           audit,
            ILogger<AuthController> log)
        {
            _uow    = uow;
            _auth   = auth;
            _config = config;
            _audit  = audit;
            _log    = log;
        }

        private string? CallerIp =>
            HttpContext.Connection.RemoteIpAddress?.ToString();

        private int AccessExpiryMinutes =>
            int.TryParse(_config["Jwt:AccessTokenExpiryMinutes"], out var m) ? m : 60;

        private int RefreshExpiryDays =>
            int.TryParse(_config["Jwt:RefreshTokenExpiryDays"], out var d) ? d : 7;

        // ── Register ──────────────────────────────────────────────────────────────
        /// <summary>Register a new account</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail(
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))));

            if (await _uow.Users.GetByUsernameAsync(dto.Username) is not null)
            {
                await _audit.LogAsync("auth.register", null, dto.Username, CallerIp,
                    success: false, details: "Username already taken");
                return Conflict(ApiResponse<object>.Fail("Username already taken"));
            }

            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                await _uow.Users.GetByEmailAsync(dto.Email) is not null)
            {
                await _audit.LogAsync("auth.register", null, dto.Username, CallerIp,
                    success: false, details: "Email already registered");
                return Conflict(ApiResponse<object>.Fail("Email already registered"));
            }

            var user = new ChatUser
            {
                Id           = ObjectId.GenerateNewId().ToString(),
                Username     = dto.Username.Trim().ToLower(),
                DisplayName  = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.Username : dto.DisplayName,
                Email        = dto.Email.Trim().ToLower(),
                AvatarUrl    = dto.AvatarUrl,
                Role         = nameof(UserRole.User),
                PasswordHash = _auth.HashPassword(dto.Password),
                CreatedAt    = DateTime.UtcNow
            };
            await _uow.Users.AddAsync(user);

            var refreshToken  = _auth.GenerateRefreshToken();
            var refreshExpiry = DateTime.UtcNow.AddDays(RefreshExpiryDays);
            await _uow.Users.SaveRefreshTokenAsync(user.Id, refreshToken, refreshExpiry);

            var accessToken = _auth.GenerateAccessToken(user);

            _log.LogInformation("New user registered userId={UserId} username={Username}",
                user.Id, user.Username);
            await _audit.LogAsync("auth.register", user.Id, user.Username, CallerIp,
                resourceId: user.Id, resourceType: "User");

            return CreatedAtAction(nameof(Me), null, ApiResponse<object>.Ok(new AuthResponseDto
            {
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
                UserId       = user.Id,
                Username     = user.Username,
                DisplayName  = user.DisplayName,
                Role         = user.Role,
                ExpiresAt    = DateTime.UtcNow.AddMinutes(AccessExpiryMinutes)
            }, "Account created"));
        }

        // ── Login ─────────────────────────────────────────────────────────────────
        /// <summary>Login with username/email and password</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail(
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))));

            var user = await _uow.Users.GetByUsernameAsync(dto.UsernameOrEmail)
                    ?? await _uow.Users.GetByEmailAsync(dto.UsernameOrEmail);

            if (user is null || !_auth.VerifyPassword(dto.Password, user.PasswordHash))
            {
                _log.LogWarning("Failed login attempt for identifier={Identifier} ip={Ip}",
                    dto.UsernameOrEmail, CallerIp);
                await _audit.LogAsync("auth.login", null, dto.UsernameOrEmail, CallerIp,
                    success: false, details: "Invalid credentials");
                return Unauthorized(ApiResponse<object>.Fail("Invalid credentials"));
            }

            var refreshToken  = _auth.GenerateRefreshToken();
            var refreshExpiry = DateTime.UtcNow.AddDays(RefreshExpiryDays);
            await _uow.Users.SaveRefreshTokenAsync(user.Id, refreshToken, refreshExpiry);
            await _uow.Users.SetStatusAsync(user.Id, UserStatus.Online);

            var accessToken = _auth.GenerateAccessToken(user);

            _log.LogInformation("Login successful userId={UserId} username={Username}",
                user.Id, user.Username);
            await _audit.LogAsync("auth.login", user.Id, user.Username, CallerIp,
                resourceId: user.Id, resourceType: "User");

            return Ok(ApiResponse<object>.Ok(new AuthResponseDto
            {
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
                UserId       = user.Id,
                Username     = user.Username,
                DisplayName  = user.DisplayName,
                Role         = user.Role,
                ExpiresAt    = DateTime.UtcNow.AddMinutes(AccessExpiryMinutes)
            }, "Login successful"));
        }

        // ── Refresh ───────────────────────────────────────────────────────────────
        /// <summary>Refresh access token using a valid refresh token</summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Refresh token is required"));

            // Single indexed query — no longer loads the entire user table (DoS fix)
            var user = await _uow.Users.GetByRefreshTokenAsync(dto.RefreshToken);

            if (user is null)
            {
                await _audit.LogAsync("auth.refresh", null, null, CallerIp,
                    success: false, details: "Invalid or expired refresh token");
                return Unauthorized(ApiResponse<object>.Fail("Invalid or expired refresh token"));
            }

            var newRefreshToken = _auth.GenerateRefreshToken();
            var refreshExpiry   = DateTime.UtcNow.AddDays(RefreshExpiryDays);
            await _uow.Users.SaveRefreshTokenAsync(user.Id, newRefreshToken, refreshExpiry);

            var accessToken = _auth.GenerateAccessToken(user);

            await _audit.LogAsync("auth.refresh", user.Id, user.Username, CallerIp,
                resourceId: user.Id, resourceType: "User");

            return Ok(ApiResponse<object>.Ok(new AuthResponseDto
            {
                AccessToken  = accessToken,
                RefreshToken = newRefreshToken,
                UserId       = user.Id,
                Username     = user.Username,
                DisplayName  = user.DisplayName,
                Role         = user.Role,
                ExpiresAt    = DateTime.UtcNow.AddMinutes(AccessExpiryMinutes)
            }, "Token refreshed"));
        }

        // ── Logout ────────────────────────────────────────────────────────────────
        /// <summary>Logout — revokes refresh token and sets user offline</summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var username = User.FindFirstValue("unique_name") ?? User.FindFirstValue(ClaimTypes.Name);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _uow.Users.SaveRefreshTokenAsync(userId, null, null);
                await _uow.Users.SetStatusAsync(userId, UserStatus.Offline);
                _log.LogInformation("Logout userId={UserId}", userId);
                await _audit.LogAsync("auth.logout", userId, username, CallerIp,
                    resourceId: userId, resourceType: "User");
            }

            return Ok(ApiResponse<object>.Ok(new { }, "Logged out"));
        }

        // ── Me ────────────────────────────────────────────────────────────────────
        /// <summary>Get the currently authenticated user's profile</summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(ApiResponse<object>.Fail("Invalid token"));

            var user = await _uow.Users.GetByIdAsync(userId);
            if (user is null) return NotFound(ApiResponse<object>.Fail("User not found"));

            return Ok(ApiResponse<object>.Ok(new
            {
                user.Id, user.Username, user.DisplayName,
                user.Email, user.AvatarUrl, user.Role,
                Status = user.Status.ToString(),
                user.IsOnline, user.LastSeen, user.CreatedAt
            }));
        }

        // ── Change Password ───────────────────────────────────────────────────────
        /// <summary>Change password — revokes all existing sessions</summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail(
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))));

            var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var username = User.FindFirstValue("unique_name") ?? User.FindFirstValue(ClaimTypes.Name);

            var user = await _uow.Users.GetByIdAsync(userId!);
            if (user is null) return NotFound(ApiResponse<object>.Fail("User not found"));

            if (!_auth.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            {
                await _audit.LogAsync("auth.change-password", userId, username, CallerIp,
                    success: false, details: "Wrong current password");
                return Unauthorized(ApiResponse<object>.Fail("Current password is incorrect"));
            }

            await _uow.Users.ChangePasswordAsync(user.Id, _auth.HashPassword(dto.NewPassword));
            await _uow.Users.SaveRefreshTokenAsync(user.Id, null, null);

            _log.LogInformation("Password changed userId={UserId}", userId);
            await _audit.LogAsync("auth.change-password", userId, username, CallerIp,
                resourceId: userId, resourceType: "User");

            return Ok(ApiResponse<object>.Ok(new { }, "Password changed. Please log in again."));
        }
    }
}
