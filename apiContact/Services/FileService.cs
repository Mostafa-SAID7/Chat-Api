namespace apiContact.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration     _config;
        private readonly ILogger<FileService> _log;

        public FileService(IWebHostEnvironment env, IConfiguration config, ILogger<FileService> log)
        {
            _env    = env;
            _config = config;
            _log    = log;
        }

        public async Task<(string url, string fileName, long size)> UploadAsync(IFormFile file)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var ext        = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var fullPath   = Path.Combine(uploadsDir, uniqueName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            _log.LogInformation("File uploaded name={UniqueName} size={Size} original={Original}",
                uniqueName, file.Length, file.FileName);

            var baseUrl = _config["App:BaseUrl"] ?? "";
            var url     = $"{baseUrl}/uploads/{uniqueName}";
            return (url, file.FileName, file.Length);
        }

        public Task<bool> DeleteAsync(string fileName)
        {
            // ── Path traversal guard ────────────────────────────────────────────
            // Reject any name that contains path separators or relative segments.
            // Only a bare file name (no directory component) is accepted.
            var sanitised = Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(sanitised) ||
                sanitised != fileName ||               // had path component stripped
                sanitised.Contains("..") ||            // belt-and-suspenders
                sanitised.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _log.LogWarning("Rejected unsafe file delete attempt: fileName={FileName}", fileName);
                return Task.FromResult(false);
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            var path       = Path.GetFullPath(Path.Combine(uploadsDir, sanitised));

            // Ensure the resolved path is still inside the uploads directory
            if (!path.StartsWith(Path.GetFullPath(uploadsDir) + Path.DirectorySeparatorChar,
                                  StringComparison.OrdinalIgnoreCase))
            {
                _log.LogWarning("Rejected path-traversal delete attempt: resolved={Path}", path);
                return Task.FromResult(false);
            }

            if (!File.Exists(path)) return Task.FromResult(false);

            File.Delete(path);
            _log.LogInformation("File deleted name={FileName}", sanitised);
            return Task.FromResult(true);
        }
    }
}
