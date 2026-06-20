namespace apiContact.Services
{
    public interface IFileService
    {
        Task<(string url, string fileName, long size)> UploadAsync(IFormFile file);
        Task<bool> DeleteAsync(string fileName);
    }
}
