using System;
using System.Globalization;

namespace CleverBudget.Core.DTOs;

public enum ExportDeliveryMode
{
    Download,
    Email,
    SignedLink
}

public enum ExportVariant
{
    Summary,
    Detailed
}

public sealed class ExportRequestOptions
{
    public CultureInfo Culture { get; init; } = CultureInfo.GetCultureInfo("pt-BR");
    public string CurrencySymbol { get; init; } = "R$";
    public ExportVariant Variant { get; init; } = ExportVariant.Detailed;
    public ExportDeliveryMode DeliveryMode { get; init; } = ExportDeliveryMode.Download;
    public string? Email { get; init; }
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromMinutes(5);
}

public sealed class ExportDeliveryResultDto
{
    public bool Delivered { get; init; }
    public ExportDeliveryMode Mode { get; init; }
    public string? Location { get; init; }
    public string? Message { get; init; }
}
