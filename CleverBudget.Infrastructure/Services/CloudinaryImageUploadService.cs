using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using CleverBudget.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CleverBudget.Infrastructure.Services;

public class CloudinaryImageUploadService : IImageUploadService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryImageUploadService> _logger;

    public CloudinaryImageUploadService(IConfiguration configuration, ILogger<CloudinaryImageUploadService> logger)
    {
        _logger = logger;

        var cloudName = configuration["Cloudinary:CloudName"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
        var apiKey = configuration["Cloudinary:ApiKey"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
        var apiSecret = configuration["Cloudinary:ApiSecret"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            _logger.LogWarning("⚠️ Cloudinary credentials not configured. Image upload will not work.");
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<ImageUploadResponse> UploadImageAsync(Stream imageStream, string fileName, string folder = "profile-photos")
    {
        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, imageStream),
                Folder = folder,
                Transformation = new Transformation()
                    .Width(500)
                    .Height(500)
                    .Crop("fill")
                    .Gravity("face"),
                Overwrite = true,
                UniqueFilename = true,
                Format = "jpg",
                // 🛡️ Moderação de conteúdo com AWS Rekognition
                Moderation = "aws_rek"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                return new ImageUploadResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Erro ao processar upload da imagem" 
                };
            }

            // 🔍 Verificar resultado da moderação
            if (uploadResult.Moderation != null && uploadResult.Moderation.Count > 0)
            {
                var moderationResult = uploadResult.Moderation[0];
                
                // Status pode ser: Approved, Rejected, Pending
                if (moderationResult.Status == ModerationStatus.Rejected)
                {
                    _logger.LogWarning("⚠️ Image rejected by moderation: {Reason}", moderationResult.Kind);
                    
                    // Deletar imagem rejeitada automaticamente
                    await DeleteImageAsync(uploadResult.PublicId);
                    
                    return new ImageUploadResponse 
                    { 
                        Success = false, 
                        ErrorMessage = "Imagem rejeitada: conteúdo impróprio detectado. Por favor, escolha outra imagem." 
                    };
                }

                _logger.LogInformation("✅ Image passed moderation: {Status}", moderationResult.Status);
            }

            _logger.LogInformation("✅ Image uploaded successfully: {Url}", uploadResult.SecureUrl);
            return new ImageUploadResponse 
            { 
                Success = true, 
                ImageUrl = uploadResult.SecureUrl.ToString() 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error uploading image to Cloudinary");
            return new ImageUploadResponse 
            { 
                Success = false, 
                ErrorMessage = "Falha ao fazer upload da imagem" 
            };
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        try
        {
            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Result == "ok")
            {
                _logger.LogInformation("✅ Image deleted successfully: {PublicId}", publicId);
                return true;
            }

            _logger.LogWarning("⚠️ Failed to delete image: {PublicId}", publicId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting image from Cloudinary");
            return false;
        }
    }
}
