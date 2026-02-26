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
    public class ImageKitFileStorageService : IFileStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _publicKey;
        private readonly string _privateKey;
        private readonly string _urlEndpoint;

        public ImageKitFileStorageService(IConfiguration configuration)
        {
            _publicKey = configuration["ImageKit:PublicKey"] ?? throw new Exception("ImageKit:PublicKey missing");
            _privateKey = configuration["ImageKit:PrivateKey"] ?? throw new Exception("ImageKit:PrivateKey missing");
            _urlEndpoint = configuration["ImageKit:UrlEndpoint"] ?? throw new Exception("ImageKit:UrlEndpoint missing");

            _httpClient = new HttpClient();
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_privateKey}:"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
        }

        public async Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string folderPath)
        {
            using var content = new MultipartFormDataContent();
            
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                await fileStream.CopyToAsync(memoryStream);
                bytes = memoryStream.ToArray();
            }

            content.Add(new ByteArrayContent(bytes), "file", fileName);
            content.Add(new StringContent(fileName), "fileName");
            content.Add(new StringContent(folderPath), "folder");
            content.Add(new StringContent("true"), "useUniqueFileName");

            var response = await _httpClient.PostAsync("https://upload.imagekit.io/api/v1/files/upload", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"ImageKit Upload Failed: {responseString}");
            }

            var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            return new FileUploadResult
            {
                FileId = root.GetProperty("fileId").GetString() ?? "",
                FileName = root.GetProperty("name").GetString() ?? "",
                FileUrl = root.GetProperty("url").GetString() ?? "",
                FileSize = root.GetProperty("size").GetInt32()
            };
        }

        public async Task<bool> DeleteFileAsync(string fileId)
        {
            var response = await _httpClient.DeleteAsync($"https://api.imagekit.io/v1/files/{fileId}");
            return response.IsSuccessStatusCode;
        }
    }
}
