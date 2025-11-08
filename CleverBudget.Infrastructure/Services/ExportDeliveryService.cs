using System;
using System.IO;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleverBudget.Infrastructure.Services;

public class ExportDeliveryService : IExportDeliveryService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ExportDeliveryService> _logger;

    public ExportDeliveryService(IEmailService emailService, ILogger<ExportDeliveryService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ExportDeliveryResultDto> DeliverAsync(string userId, string artifactName, byte[] payload, ExportRequestOptions options)
    {
        if (options.DeliveryMode == ExportDeliveryMode.Download)
        {
            return new ExportDeliveryResultDto
            {
                Delivered = false,
                Mode = ExportDeliveryMode.Download,
                Message = "Nenhuma entrega necessária; download direto será realizado."
            };
        }

        if (options.DeliveryMode == ExportDeliveryMode.Email)
        {
            if (string.IsNullOrWhiteSpace(options.Email))
            {
                return new ExportDeliveryResultDto
                {
                    Delivered = false,
                    Mode = ExportDeliveryMode.Email,
                    Message = "Email do destinatário não informado."
                };
            }

            var subject = $"Seu arquivo {artifactName} está pronto";
            var htmlContent = $"<p>Olá!</p><p>O arquivo <strong>{artifactName}</strong> solicitado foi gerado com sucesso.</p><p>Ele está anexado a este email.</p>";
            var sent = await _emailService.SendEmailAsync(options.Email, subject, htmlContent, payload, artifactName);

            return new ExportDeliveryResultDto
            {
                Delivered = sent,
                Mode = ExportDeliveryMode.Email,
                Message = sent ? "Arquivo enviado por email." : "Falha ao enviar email com o arquivo."
            };
        }

        if (options.DeliveryMode == ExportDeliveryMode.SignedLink)
        {
            try
            {
                var root = Path.Combine(AppContext.BaseDirectory, "Backups", "Exports");
                Directory.CreateDirectory(root);

                var token = Guid.NewGuid().ToString("N");
                var fileName = $"{Path.GetFileNameWithoutExtension(artifactName)}-{token}{Path.GetExtension(artifactName)}";
                var fullPath = Path.Combine(root, fileName);

                await File.WriteAllBytesAsync(fullPath, payload);

                var location = $"exports/{fileName}";

                return new ExportDeliveryResultDto
                {
                    Delivered = true,
                    Mode = ExportDeliveryMode.SignedLink,
                    Location = location,
                    Message = "Arquivo disponível para download protegido."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar link assinado para {Artifact}", artifactName);

                return new ExportDeliveryResultDto
                {
                    Delivered = false,
                    Mode = ExportDeliveryMode.SignedLink,
                    Message = "Falha ao gerar link assinado."
                };
            }
        }

        return new ExportDeliveryResultDto
        {
            Delivered = false,
            Mode = options.DeliveryMode,
            Message = "Modo de entrega não suportado."
        };
    }
}
