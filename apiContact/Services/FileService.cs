namespace apiContact.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public FileService(IWebHostEnvironment env, IConfiguration config)
        {
            _env = env;
            _config = config;
        }

        public async Task<(string url, string fileName, long size)> UploadAsync(IFormFile file)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(file.FileName);
            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsDir, uniqueName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            var baseUrl = _config["App:BaseUrl"] ?? "";
            var url = $"{baseUrl}/uploads/{uniqueName}";
            return (url, file.FileName, file.Length);
        }

        public Task<bool> DeleteAsync(string fileName)
        {
            var path = Path.Combine(_env.WebRootPath, "uploads", fileName);
            if (File.Exists(path)) { File.Delete(path); return Task.FromResult(true); }
            return Task.FromResult(false);
        }
    }
}
