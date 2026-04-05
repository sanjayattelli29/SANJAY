using System.IO;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// This interface defines how we save files (like PDFs or pictures) to the internet.
    /// We use this to store policy documents, claim bills, and profile pictures.
    /// </summary>
    public interface IFileStorageService
    {
        // Uploads a file to a storage provider (like ImageKit) and returns the web link (URL)
        Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string folderPath);
        
        // Deletes a file from the storage provider using its ID
        Task<bool> DeleteFileAsync(string fileId);
    }

    /// <summary>
    /// This class holds the information we get back after successfully uploading a file.
    /// </summary>
    public class FileUploadResult
    {
        // The unique ID given to the file by the storage service
        public string FileId { get; set; } = string.Empty;
        
        // The original name of the file (e.g., 'my_bill.pdf')
        public string FileName { get; set; } = string.Empty;
        
        // The public web link where the file can be viewed or downloaded
        public string FileUrl { get; set; } = string.Empty;
        
        // The size of the file in bytes
        public long FileSize { get; set; }
    }
}
