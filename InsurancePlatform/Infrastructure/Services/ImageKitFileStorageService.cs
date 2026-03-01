using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json; // for json parsing
using System.Threading.Tasks; // async toolkit

namespace Infrastructure.Services
{
    // this class is for saving files on the internet using imagekit
    public class ImageKitFileStorageService : IFileStorageService // implementation class
    {
        private readonly HttpClient _httpClient;
        private readonly string _publicKey;
        private readonly string _privateKey;
        private readonly string _urlEndpoint;

        public ImageKitFileStorageService(IConfiguration configuration)
        {
            // read settings from appsettings.json
            _publicKey = configuration["ImageKit:PublicKey"] ?? throw new Exception("ImageKit:PublicKey missing");
            _privateKey = configuration["ImageKit:PrivateKey"] ?? throw new Exception("ImageKit:PrivateKey missing"); // secret key
            _urlEndpoint = configuration["ImageKit:UrlEndpoint"] ?? throw new Exception("ImageKit:UrlEndpoint missing");

            _httpClient = new HttpClient();
            // setup authorization for imagekit
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_privateKey}:"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue); // set auth
        }

        // code to upload a file
        public async Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string folderPath)
        {
            using var content = new MultipartFormDataContent(); // form data
            
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                await fileStream.CopyToAsync(memoryStream);
                bytes = memoryStream.ToArray();
            }

            // add file data to request
            content.Add(new ByteArrayContent(bytes), "file", fileName);
            content.Add(new StringContent(fileName), "fileName");
            content.Add(new StringContent(folderPath), "folder");
            content.Add(new StringContent("true"), "useUniqueFileName");

            var response = await _httpClient.PostAsync("https://upload.imagekit.io/api/v1/files/upload", content); // api call
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
            var response = await _httpClient.DeleteAsync($"https://api.imagekit.io/v1/files/{fileId}"); // api call
            return response.IsSuccessStatusCode;
        }
    }
}
// imagekit storage service end
