using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MedLinkPortal.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary? _cloudinary;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CloudinaryService> _logger;
        private readonly bool _isConfigured;

        public CloudinaryService(IConfiguration config, IWebHostEnvironment env, ILogger<CloudinaryService> logger)
        {
            _env = env;
            _logger = logger;

            // Try reading from CloudinarySettings section or direct environment variables (Railway)
            var cloudName = config["CloudinarySettings:CloudName"] ?? config["CLOUDINARY_CLOUD_NAME"];
            var apiKey = config["CloudinarySettings:ApiKey"] ?? config["CLOUDINARY_API_KEY"];
            var apiSecret = config["CloudinarySettings:ApiSecret"] ?? config["CLOUDINARY_API_SECRET"];
            var url = config["CLOUDINARY_URL"];

            if (!string.IsNullOrEmpty(url))
            {
                _cloudinary = new Cloudinary(url);
                _cloudinary.Api.Secure = true;
                _isConfigured = true;
            }
            else if (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
            {
                var account = new Account(cloudName, apiKey, apiSecret);
                _cloudinary = new Cloudinary(account);
                _cloudinary.Api.Secure = true;
                _isConfigured = true;
            }
            else
            {
                _isConfigured = false;
                _logger.LogWarning("Cloudinary is not configured. Falling back to local disk storage. Set CloudinarySettings in appsettings or Railway env variables.");
            }
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folder = "medlink_uploads")
        {
            if (file == null || file.Length == 0) return null;

            if (!_isConfigured || _cloudinary == null)
            {
                // Fallback to local storage if Cloudinary credentials are not set
                return await SaveToLocalStorageAsync(file, folder);
            }

            try
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation("Successfully uploaded image to Cloudinary: {Url}", uploadResult.SecureUrl.ToString());
                    return uploadResult.SecureUrl.ToString();
                }

                _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error?.Message);
                return await SaveToLocalStorageAsync(file, folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Cloudinary upload. Falling back to local disk storage.");
                return await SaveToLocalStorageAsync(file, folder);
            }
        }

        public async Task<string?> UploadRawFileAsync(IFormFile file, string folder = "medlink_docs")
        {
            if (file == null || file.Length == 0) return null;

            if (!_isConfigured || _cloudinary == null)
            {
                return await SaveToLocalStorageAsync(file, folder);
            }

            try
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation("Successfully uploaded raw file to Cloudinary: {Url}", uploadResult.SecureUrl.ToString());
                    return uploadResult.SecureUrl.ToString();
                }

                _logger.LogError("Cloudinary raw upload failed: {Error}", uploadResult.Error?.Message);
                return await SaveToLocalStorageAsync(file, folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Cloudinary raw upload. Falling back to local disk.");
                return await SaveToLocalStorageAsync(file, folder);
            }
        }

        public async Task<bool> DeleteFileAsync(string publicId)
        {
            if (!_isConfigured || _cloudinary == null || string.IsNullOrEmpty(publicId)) return false;

            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from Cloudinary: {PublicId}", publicId);
                return false;
            }
        }

        private async Task<string> SaveToLocalStorageAsync(IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", folder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folder}/{uniqueFileName}";
        }
    }
}
