using System.IO;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    // this interface is for saving and deleting files like medical bills
    public interface IFileStorageService
    {
        // upload a new file to the cloud storage
        Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string folderPath);
        
        // delete an old file from storage
        Task<bool> DeleteFileAsync(string fileId);
    }

    // this class gives the result of file upload
    public class FileUploadResult
    {
        // unique id of file from cloud service
        public string FileId { get; set; } = string.Empty;
        // name of the file saved
        public string FileName { get; set; } = string.Empty;
        // web link to download or see file
        public string FileUrl { get; set; } = string.Empty;
        // how big the file is
        public long FileSize { get; set; }
    }
}
