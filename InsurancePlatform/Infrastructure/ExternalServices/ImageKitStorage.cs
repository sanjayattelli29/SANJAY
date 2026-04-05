using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.ExternalServices
{
    // This client handles file upload and deletion using ImageKit cloud storage
    /// <summary>
    /// This service handles "Cloud Storage" for all our files.
    /// When a user uploads a photo of their Aadhaar card or a medical bill, 
    /// this class sends it to "ImageKit", which stores it safely on the internet.
    /// </summary>
    public class ImageKitStorage : IFileStorageService
    {
        // These are the secret keys and settings we need to talk to ImageKit.
        private readonly HttpClient _httpClient;
        private readonly string _publicKey;
        private readonly string _privateKey;
        private readonly string _urlEndpoint;

        // The constructor sets up the security credentials for ImageKit.
        public ImageKitStorage(IConfiguration configuration)
        {
            // We get the keys from our configuration (appsettings.json).
            _publicKey = configuration["ImageKit:PublicKey"] ?? throw new Exception("ImageKit:PublicKey missing");
            _privateKey = configuration["ImageKit:PrivateKey"] ?? throw new Exception("ImageKit:PrivateKey missing");
            _urlEndpoint = configuration["ImageKit:UrlEndpoint"] ?? throw new Exception("ImageKit:UrlEndpoint missing");

            _httpClient = new HttpClient();

            // We create a "Basic Auth" header using the private key to prove it's really our app.
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_privateKey}:"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
        }

        /// <summary>
        /// This method uploads a file from our server to the ImageKit cloud.
        /// </summary>
        public async Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string folderPath)
        {
            // We use MultipartFormDataContent to bundle the file and its settings together.
            using var content = new MultipartFormDataContent();

            // Convert the file stream into a byte array (the raw data of the file).
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                await fileStream.CopyToAsync(memoryStream);
                bytes = memoryStream.ToArray();
            }

            // Add the file and instructions (like which folder to put it in).
            content.Add(new ByteArrayContent(bytes), "file", fileName);
            content.Add(new StringContent(fileName), "fileName");
            content.Add(new StringContent(folderPath), "folder");
            content.Add(new StringContent("true"), "useUniqueFileName"); // Add a random code to the filename to avoid duplicates.

            // Send the file to the ImageKit API.
            var response = await _httpClient.PostAsync("https://upload.imagekit.io/api/v1/files/upload", content);
            var responseString = await response.Content.ReadAsStringAsync();

            // If it fails, tell us exactly why.
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"ImageKit Upload Failed: {responseString}");
            }

            // If it succeeds, parse the JSON response from ImageKit.
            var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            // Return the key details (ID, Name, and final Web URL) so we can save them in our database.
            return new FileUploadResult
            {
                FileId = root.GetProperty("fileId").GetString() ?? "",
                FileName = root.GetProperty("name").GetString() ?? "",
                FileUrl = root.GetProperty("url").GetString() ?? "",
                FileSize = root.GetProperty("size").GetInt32()
            };
        }

        /// <summary>
        /// This method deletes a file from the cloud when it's no longer needed.
        /// </summary>
        public async Task<bool> DeleteFileAsync(string fileId)
        {
            var response = await _httpClient.DeleteAsync($"https://api.imagekit.io/v1/files/{fileId}");
            return response.IsSuccessStatusCode;
        }
    }
}
