using System.Security.Claims;
using apiContact.Hubs;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using apiContact.Models.Enums;
using apiContact.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace apiContact.Controllers
{
    [ApiController]
    [Route("api/files")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IFileService              _files;
        private readonly IMessageService           _messages;
        private readonly IHubContext<ChatHub>      _hub;
        private readonly IAuditService             _audit;
        private readonly ILogger<FilesController>  _log;

        public FilesController(
            IFileService             files,
            IMessageService          messages,
            IHubContext<ChatHub>     hub,
            IAuditService            audit,
            ILogger<FilesController> log)
        {
            _files    = files;
            _messages = messages;
            _hub      = hub;
            _audit    = audit;
            _log      = log;
        }

        private string? CallerIp => HttpContext.Connection.RemoteIpAddress?.ToString();

        private string? CallerId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        private string CallerName =>
            User.FindFirstValue("displayName")
         ?? User.FindFirstValue(ClaimTypes.Name)
         ?? "Unknown";

        // ── Upload ────────────────────────────────────────────────────────────────
        /// <summary>Upload a file (max 20 MB). Optionally attach to a room as a message.</summary>
        [HttpPost("upload")]
        [RequestSizeLimit(20_000_000)]
        [EnableRateLimiting("files")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? roomId)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<object>.Fail("No file provided"));

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp",
                                  ".pdf", ".txt", ".zip", ".mp4", ".mp3" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                await _audit.LogAsync("file.upload", CallerId, CallerName, CallerIp,
                    success: false, details: $"Rejected file type: {ext}");
                return BadRequest(ApiResponse<object>.Fail($"File type '{ext}' not allowed"));
            }

            var (url, fileName, size) = await _files.UploadAsync(file);

            await _audit.LogAsync("file.upload", CallerId, CallerName, CallerIp,
                resourceId: fileName, resourceType: "File",
                details: $"size={size} room={roomId ?? "none"}");

            object result = new { url, fileName, size, ext };

            if (!string.IsNullOrWhiteSpace(roomId))
            {
                var isImage = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext);
                var msg = await _messages.SendAsync(
                    new SendMessageDto
                    {
                        RoomId   = roomId,
                        SenderId = CallerId!,
                        Content  = fileName,
                        Type     = isImage ? MessageType.Image : MessageType.File
                    },
                    senderName: CallerName);

                msg.FileUrl  = url;
                msg.FileName = fileName;
                msg.FileSize = size;

                await _hub.Clients.Group(roomId).SendAsync("ReceiveMessage", new
                {
                    msg.Id, msg.RoomId, msg.SenderId, msg.SenderName,
                    msg.Content, Type = msg.Type.ToString(),
                    msg.FileUrl, msg.FileName, msg.FileSize, msg.Timestamp
                });

                result = new { url, fileName, size, ext, messageId = msg.Id };
            }

            return Ok(ApiResponse<object>.Ok(result, "File uploaded"));
        }

        // ── Delete ────────────────────────────────────────────────────────────────
        /// <summary>Delete an uploaded file by stored filename</summary>
        [HttpDelete("{fileName}")]
        public async Task<IActionResult> Delete(string fileName)
        {
            // Validate the file name looks like a safe GUID-based name before
            // passing to the service (service also validates, this is defence-in-depth)
            var sanitised = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(sanitised) || sanitised != fileName)
            {
                _log.LogWarning("Delete rejected unsafe fileName={FileName} caller={UserId}",
                    fileName, CallerId);
                await _audit.LogAsync("file.delete", CallerId, CallerName, CallerIp,
                    success: false, resourceType: "File", details: $"Unsafe filename: {fileName}");
                return BadRequest(ApiResponse<object>.Fail("Invalid file name"));
            }

            var ok = await _files.DeleteAsync(sanitised);
            if (!ok)
            {
                await _audit.LogAsync("file.delete", CallerId, CallerName, CallerIp,
                    success: false, resourceId: sanitised, resourceType: "File",
                    details: "File not found or rejected");
                return NotFound(ApiResponse<object>.Fail("File not found"));
            }

            await _audit.LogAsync("file.delete", CallerId, CallerName, CallerIp,
                resourceId: sanitised, resourceType: "File");

            return Ok(ApiResponse<object>.Ok(new { fileName = sanitised }, "File deleted"));
        }
    }
}
