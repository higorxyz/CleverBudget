using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CleverBudget.Infrastructure.Helpers;

/// <summary>
/// Helper para criação de documentos PDF padronizados
/// </summary>
public static class PdfHelper
{
    static PdfHelper()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Cria o cabeçalho padrão dos relatórios
    /// </summary>
    public static void CreateHeader(IContainer container, string title, string subtitle = "")
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(text =>
                {
                    text.Span("💼 CleverBudget").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                });

                column.Item().Text(title).FontSize(18).SemiBold();

                if (!string.IsNullOrEmpty(subtitle))
                {
                    column.Item().Text(subtitle).FontSize(12).FontColor(Colors.Grey.Darken1);
                }
            });

            row.ConstantItem(100).AlignRight().Text(text =>
            {
                text.Span("Data: ").SemiBold();
                text.Span(DateTime.Now.ToString("dd/MM/yyyy"));
            });
        });
    }

    /// <summary>
    /// Cria o rodapé padrão
    /// </summary>
    public static void CreateFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Página ").FontSize(10).FontColor(Colors.Grey.Darken1);
            text.CurrentPageNumber().FontSize(10).FontColor(Colors.Grey.Darken1);
            text.Span(" de ").FontSize(10).FontColor(Colors.Grey.Darken1);
            text.TotalPages().FontSize(10).FontColor(Colors.Grey.Darken1);
            text.Span(" • Gerado por CleverBudget API").FontSize(10).FontColor(Colors.Grey.Darken1);
        });
    }

    /// <summary>
    /// Formata valor monetário
    /// </summary>
    public static string FormatCurrency(decimal value)
    {
        return value.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    }

    /// <summary>
    /// Obtém cor baseada no tipo de transação
    /// </summary>
    public static string GetTransactionColor(string type)
    {
        return type switch
        {
            "Income" => Colors.Green.Medium,
            "Expense" => Colors.Red.Medium,
            _ => Colors.Grey.Medium
        };
    }

    /// <summary>
    /// Obtém cor baseada no status da meta
    /// </summary>
    public static string GetGoalStatusColor(string status)
    {
        return status switch
        {
            "OnTrack" => Colors.Green.Medium,
            "Warning" => Colors.Orange.Medium,
            "Exceeded" => Colors.Red.Medium,
            _ => Colors.Grey.Medium
        };
    }
}