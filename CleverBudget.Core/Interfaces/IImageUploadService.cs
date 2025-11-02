namespace CleverBudget.Core.Interfaces;

public class ImageUploadResponse
{
    public bool Success { get; set; }
    public string? ImageUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IImageUploadService
{
    Task<ImageUploadResponse> UploadImageAsync(Stream imageStream, string fileName, string folder = "profile-photos");
    Task<bool> DeleteImageAsync(string publicId);
}
