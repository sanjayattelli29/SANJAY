using System.IO;
using System.Threading.Tasks;

namespace Application.Interfaces.Infrastructure
{
    public interface IFileStorageService
    {
        Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string folderPath);
        Task<bool> DeleteFileAsync(string fileId);
    }

    public class FileUploadResult
    {
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }
}
