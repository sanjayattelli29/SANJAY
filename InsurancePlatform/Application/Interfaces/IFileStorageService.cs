using System.IO; // for stream handling
using System.Threading.Tasks; // for async support

namespace Application.Interfaces // interface folder
{
    // this interface is for saving and deleting files like medical bills
    public interface IFileStorageService // storage service contract
    {
        // upload a new file to the cloud storage
        Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string folderPath); // send to cloud
        
        // delete an old file from storage
        Task<bool> DeleteFileAsync(string fileId); // remove from cloud
    }

    // this class gives the result of file upload
    public class FileUploadResult // response from storage
    {
        // unique id of file from cloud service
        public string FileId { get; set; } = string.Empty;
        // name of the file saved
        public string FileName { get; set; } = string.Empty;
        // web link to download or see file
        public string FileUrl { get; set; } = string.Empty;
        // how big the file is
        public long FileSize { get; set; } // byte count
    }
}
// file storage interface ends
