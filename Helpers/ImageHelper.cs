using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace ScspApi.Helpers
{
    public class ImageHelper
    {
        private readonly IWebHostEnvironment _env;

        public ImageHelper(IWebHostEnvironment env){
            _env = env;
        }

        public async Task<string> SaveImageAsync(IFormFile file, string folder = "problems"){
            var uniqueFileName = GenerateUniqueFileName(file.FileName);

            var uploadPath = Path.Combine(_env.WebRootPath, folder);

            if (!Directory.Exists(uploadPath)){
                Directory.CreateDirectory(uploadPath);
            }

            var filePath = Path.Combine(uploadPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/{folder}/{uniqueFileName}";
        }

        private string GenerateUniqueFileName(string originalFileName){
            var fileExtension = Path.GetExtension(originalFileName);
            var uniqueName = Guid.NewGuid().ToString("N") + fileExtension;
            return uniqueName;
        }

        public static bool IsValidImage(string fileName, Stream fileStream){
            // Extensões permitidas
            var allowedExtensions = new List<string> { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(fileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                return false;

            var mimeType = GetMimeType(fileStream);
            var allowedMimeTypes = new List<string> { "image/jpeg", "image/png", "image/gif" };

            return allowedMimeTypes.Contains(mimeType);
        }

        private static string GetMimeType(Stream stream)
        {
            using (var reader = new BinaryReader(stream)){
                byte[] buffer = reader.ReadBytes(4);
                if (buffer[0] == 0xFF && buffer[1] == 0xD8)
                    return "image/jpeg"; // JPEG
                if (buffer[0] == 0x89 && buffer[1] == 0x50)
                    return "image/png";  // PNG
                if (buffer[0] == 0x47 && buffer[1] == 0x49)
                    return "image/gif";  // GIF
            }
            return "unknown";
        }

    }
}
