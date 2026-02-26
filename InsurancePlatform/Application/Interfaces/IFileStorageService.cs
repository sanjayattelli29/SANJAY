using System.IO;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Uploads a file to secure storage.
        /// </summary>
        /// <param name="fileStream">Stream of the file content.</param>
        /// <param name="fileName">Original name of the file.</param>
        /// <param name="folderPath">Path/folder where to store the file.</param>
        /// <returns>A result containing FileId, FileName, and FileUrl.</returns>
        Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string folderPath);
        
        /// <summary>
        /// Deletes a file from secure storage.
        /// </summary>
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
