using System.ComponentModel.DataAnnotations;
using apiContact.Models.Enums;

namespace apiContact.Models.Dtos
{
    // ── Auth ──────────────────────────────────────────────────
    public class RegisterDto
    {
        [Required]
        [MinLength(3,  ErrorMessage = "Username must be at least 3 characters")]
        [MaxLength(32, ErrorMessage = "Username must be at most 32 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_\-\.]+$",
            ErrorMessage = "Username may only contain letters, numbers, underscores, hyphens, and dots")]
        public string Username    { get; set; } = string.Empty;

        [MaxLength(64, ErrorMessage = "Display name must be at most 64 characters")]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Must be a valid email address")]
        [MaxLength(254, ErrorMessage = "Email must be at most 254 characters")]
        public string Email       { get; set; } = string.Empty;

        [Required]
        [MinLength(8,   ErrorMessage = "Password must be at least 8 characters")]
        [MaxLength(128, ErrorMessage = "Password must be at most 128 characters")]
        public string Password    { get; set; } = string.Empty;

        [Url(ErrorMessage = "AvatarUrl must be a valid URL")]
        [MaxLength(512, ErrorMessage = "AvatarUrl must be at most 512 characters")]
        public string? AvatarUrl  { get; set; }
    }

    public class LoginDto
    {
        [Required]
        [MaxLength(254, ErrorMessage = "Username or email must be at most 254 characters")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(128, ErrorMessage = "Password must be at most 128 characters")]
        public string Password        { get; set; } = string.Empty;
    }

    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        [Required]
        [MaxLength(128)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8,   ErrorMessage = "New password must be at least 8 characters")]
        [MaxLength(128, ErrorMessage = "New password must be at most 128 characters")]
        public string NewPassword     { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string   AccessToken  { get; set; } = string.Empty;
        public string   RefreshToken { get; set; } = string.Empty;
        public string   UserId       { get; set; } = string.Empty;
        public string   Username     { get; set; } = string.Empty;
        public string   DisplayName  { get; set; } = string.Empty;
        public string   Role         { get; set; } = string.Empty;
        public DateTime ExpiresAt    { get; set; }
    }

    // ── User profile management ───────────────────────────────
    public class CreateUserDto
    {
        [Required]
        [MinLength(3)]
        [MaxLength(32)]
        public string Username    { get; set; } = string.Empty;

        [MaxLength(64)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email       { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        public string Password    { get; set; } = string.Empty;

        [Url]
        [MaxLength(512)]
        public string? AvatarUrl  { get; set; }

        public string Role        { get; set; } = nameof(UserRole.User);
    }

    public class UpdateUserDto
    {
        [MaxLength(64)]
        public string? DisplayName { get; set; }

        [Url]
        [MaxLength(512)]
        public string? AvatarUrl   { get; set; }

        /// <summary>Deprecated — use SetStatusDto for explicit presence control</summary>
        public bool? IsOnline { get; set; }
    }

    /// <summary>Set a user's presence status explicitly</summary>
    public class SetStatusDto
    {
        public UserStatus Status { get; set; } = UserStatus.Online;
    }
}
