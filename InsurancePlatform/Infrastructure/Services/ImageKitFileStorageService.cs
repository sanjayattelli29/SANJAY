using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    // This service handles file upload and deletion using ImageKit cloud storage
    // It implements the IFileStorageService interface from Application layer
    public class ImageKitFileStorageService : IFileStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _publicKey;
        private readonly string _privateKey;
        private readonly string _urlEndpoint;

        // Constructor reads ImageKit configuration and prepares HttpClient with authentication
        public ImageKitFileStorageService(IConfiguration configuration)
        {
            // Read ImageKit settings from appsettings.json
            _publicKey = configuration["ImageKit:PublicKey"] ?? throw new Exception("ImageKit:PublicKey missing");
            _privateKey = configuration["ImageKit:PrivateKey"] ?? throw new Exception("ImageKit:PrivateKey missing");
            _urlEndpoint = configuration["ImageKit:UrlEndpoint"] ?? throw new Exception("ImageKit:UrlEndpoint missing");

            // Create HttpClient for sending API requests
            _httpClient = new HttpClient();

            // Create Basic authentication header using private key
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_privateKey}:"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
        }

        // This function uploads a file to ImageKit storage
        public async Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string folderPath)
        {
            // Prepare multipart form request for file upload
            using var content = new MultipartFormDataContent();

            byte[] bytes;

            // Convert file stream into byte array
            using (var memoryStream = new MemoryStream())
            {
                await fileStream.CopyToAsync(memoryStream);
                bytes = memoryStream.ToArray();
            }

            // Add file data and metadata to request body
            content.Add(new ByteArrayContent(bytes), "file", fileName);
            content.Add(new StringContent(fileName), "fileName");
            content.Add(new StringContent(folderPath), "folder");
            content.Add(new StringContent("true"), "useUniqueFileName");

            // Send POST request to ImageKit upload API
            var response = await _httpClient.PostAsync("https://upload.imagekit.io/api/v1/files/upload", content);

            // Read API response as string
            var responseString = await response.Content.ReadAsStringAsync();

            // If upload fails, throw exception with error message
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"ImageKit Upload Failed: {responseString}");
            }

            // Parse JSON response from ImageKit
            var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            // Return upload result with file details
            return new FileUploadResult
            {
                FileId = root.GetProperty("fileId").GetString() ?? "",
                FileName = root.GetProperty("name").GetString() ?? "",
                FileUrl = root.GetProperty("url").GetString() ?? "",
                FileSize = root.GetProperty("size").GetInt32()
            };
        }

        // This function deletes a file from ImageKit using fileId
        public async Task<bool> DeleteFileAsync(string fileId)
        {
            // Send DELETE request to ImageKit API
            var response = await _httpClient.DeleteAsync($"https://api.imagekit.io/v1/files/{fileId}");

            // Return true if deletion successful
            return response.IsSuccessStatusCode;
        }
    }
}