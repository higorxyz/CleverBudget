using CleverBudget.Core.DTOs;

namespace CleverBudget.Core.Interfaces;

public interface IExportDeliveryService
{
    Task<ExportDeliveryResultDto> DeliverAsync(string userId, string artifactName, byte[] payload, ExportRequestOptions options);
}
