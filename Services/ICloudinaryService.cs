using Microsoft.AspNetCore.Http;

namespace MedLinkPortal.Services
{
    public interface ICloudinaryService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folder = "medlink_uploads");
        Task<string?> UploadRawFileAsync(IFormFile file, string folder = "medlink_docs");
        Task<bool> DeleteFileAsync(string publicId);
    }
}
