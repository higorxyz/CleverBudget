using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Helpers;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;

namespace CleverBudget.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly AppDbContext _context;

    public ExportService(AppDbContext context)
    {
        _context = context;
    }

    #region CSV Exports (já existentes)

    public async Task<byte[]> ExportTransactionsToCsvAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.Now.AddMonths(-12);
        var end = endDate ?? DateTime.Now;

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= start && t.Date <= end)
            .OrderByDescending(t => t.Date)
            .Select(t => new
            {
                Data = t.Date.ToString("dd/MM/yyyy"),
                Tipo = t.Type.ToString(),
                Valor = t.Amount,
                Categoria = t.Category.Name,
                Descricao = t.Description
            })
            .ToListAsync();

        return GenerateCsv(transactions);
    }

    public async Task<byte[]> ExportCategoriesToCsvAsync(string userId)
    {
        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                Nome = c.Name,
                Icone = c.Icon ?? "",
                Cor = c.Color ?? "",
                Padrao = c.IsDefault ? "Sim" : "Não",
                CriadaEm = c.CreatedAt.ToString("dd/MM/yyyy")
            })
            .ToListAsync();

        return GenerateCsv(categories);
    }

    public async Task<byte[]> ExportGoalsToCsvAsync(string userId, int? month = null, int? year = null)
    {
        var query = _context.Goals
            .Include(g => g.Category)
            .Where(g => g.UserId == userId);

        if (month.HasValue)
            query = query.Where(g => g.Month == month.Value);

        if (year.HasValue)
            query = query.Where(g => g.Year == year.Value);

        var goals = await query
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Month)
            .Select(g => new
            {
                Mes = g.Month,
                Ano = g.Year,
                Categoria = g.Category.Name,
                MetaValor = g.TargetAmount,
                CriadaEm = g.CreatedAt.ToString("dd/MM/yyyy")
            })
            .ToListAsync();

        return GenerateCsv(goals);
    }

    private byte[] GenerateCsv<T>(IEnumerable<T> records)
    {
        using var memoryStream = new MemoryStream();
        using var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.GetCultureInfo("pt-BR"))
        {
            Delimiter = ";",
            HasHeaderRecord = true
        });

        csvWriter.WriteRecords(records);
        streamWriter.Flush();

        return memoryStream.ToArray();
    }

    #endregion

    #region PDF Exports (novos)

    /// <summary>
    /// Exporta lista de transações para PDF
    /// </summary>
    public async Task<byte[]> ExportTransactionsToPdfAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.Now.AddMonths(-1);
        var end = endDate ?? DateTime.Now;

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= start && t.Date <= end)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().Element(c => PdfHelper.CreateHeader(
                    c,
                    "Relatório de Transações",
                    $"Período: {start:dd/MM/yyyy} a {end:dd/MM/yyyy}"
                ));

                page.Content().Column(column =>
                {
                    var totalIncome = transactions.Where(t => t.Type == Core.Enums.TransactionType.Income).Sum(t => t.Amount);
                    var totalExpense = transactions.Where(t => t.Type == Core.Enums.TransactionType.Expense).Sum(t => t.Amount);
                    var balance = totalIncome - totalExpense;

                    column.Item().PaddingVertical(10).Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Green.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Receitas").FontSize(12).SemiBold();
                            col.Item().Text(PdfHelper.FormatCurrency(totalIncome)).FontSize(16).SemiBold().FontColor(Colors.Green.Darken2);
                        });

                        row.RelativeItem().Background(Colors.Red.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Despesas").FontSize(12).SemiBold();
                            col.Item().Text(PdfHelper.FormatCurrency(totalExpense)).FontSize(16).SemiBold().FontColor(Colors.Red.Darken2);
                        });

                        row.RelativeItem().Background(Colors.Blue.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Saldo").FontSize(12).SemiBold();
                            col.Item().Text(PdfHelper.FormatCurrency(balance)).FontSize(16).SemiBold().FontColor(balance >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                        });
                    });

                    column.Item().PaddingVertical(10);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(5);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Data").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Tipo").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Categoria").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Descrição").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Valor").SemiBold();
                        });

                        foreach (var transaction in transactions)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(transaction.Date.ToString("dd/MM/yyyy"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(transaction.Type == Core.Enums.TransactionType.Income ? "Receita" : "Despesa");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(transaction.Category.Name);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(transaction.Description);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                .Text(PdfHelper.FormatCurrency(transaction.Amount))
                                .FontColor(transaction.Type == Core.Enums.TransactionType.Income ? Colors.Green.Darken2 : Colors.Red.Darken2);
                        }
                    });

                    if (!transactions.Any())
                    {
                        column.Item().PaddingVertical(20).AlignCenter().Text("Nenhuma transação encontrada no período.").FontSize(14).Italic().FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Footer().Element(PdfHelper.CreateFooter);
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Exporta relatório financeiro completo para PDF
    /// </summary>
    public async Task<byte[]> ExportFinancialReportToPdfAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.Now.AddMonths(-1);
        var end = endDate ?? DateTime.Now;

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= start && t.Date <= end)
            .ToListAsync();

        var totalIncome = transactions.Where(t => t.Type == Core.Enums.TransactionType.Income).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.Type == Core.Enums.TransactionType.Expense).Sum(t => t.Amount);
        var balance = totalIncome - totalExpense;

        var expensesByCategory = transactions
            .Where(t => t.Type == Core.Enums.TransactionType.Expense)
            .GroupBy(t => t.Category.Name)
            .Select(g => new
            {
                Category = g.Key,
                Total = g.Sum(t => t.Amount),
                Count = g.Count(),
                Percentage = totalExpense > 0 ? (g.Sum(t => t.Amount) / totalExpense) * 100 : 0
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().Element(c => PdfHelper.CreateHeader(
                    c,
                    "Relatório Financeiro Completo",
                    $"Período: {start:dd/MM/yyyy} a {end:dd/MM/yyyy}"
                ));

                page.Content().Column(column =>
                {
                    column.Item().PaddingBottom(15).Text("📊 Resumo Geral").FontSize(16).SemiBold();

                    column.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Green.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Total de Receitas").FontSize(12);
                            col.Item().Text(PdfHelper.FormatCurrency(totalIncome)).FontSize(18).SemiBold().FontColor(Colors.Green.Darken2);
                        });

                        row.RelativeItem().Background(Colors.Red.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Total de Despesas").FontSize(12);
                            col.Item().Text(PdfHelper.FormatCurrency(totalExpense)).FontSize(18).SemiBold().FontColor(Colors.Red.Darken2);
                        });

                        row.RelativeItem().Background(Colors.Blue.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Saldo Final").FontSize(12);
                            col.Item().Text(PdfHelper.FormatCurrency(balance)).FontSize(18).SemiBold()
                                .FontColor(balance >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                        });
                    });

                    column.Item().PaddingVertical(15);

                    column.Item().PaddingBottom(10).Text("🗂️ Despesas por Categoria").FontSize(16).SemiBold();

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Categoria").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Transações").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Total").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("%").SemiBold();
                        });

                        foreach (var item in expensesByCategory)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Category);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Count.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(PdfHelper.FormatCurrency(item.Total)).FontColor(Colors.Red.Darken2);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{item.Percentage:F1}%");
                        }
                    });

                    if (!expensesByCategory.Any())
                    {
                        column.Item().PaddingVertical(10).AlignCenter().Text("Nenhuma despesa registrada no período.").Italic().FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Footer().Element(PdfHelper.CreateFooter);
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Exporta relatório de metas para PDF
    /// </summary>
    public async Task<byte[]> ExportGoalsReportToPdfAsync(string userId, int? month = null, int? year = null)
    {
        var currentMonth = month ?? DateTime.Now.Month;
        var currentYear = year ?? DateTime.Now.Year;

        var goals = await _context.Goals
            .Include(g => g.Category)
            .Where(g => g.UserId == userId && g.Month == currentMonth && g.Year == currentYear)
            .ToListAsync();

        var startDate = new DateTime(currentYear, currentMonth, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var goalsWithProgress = new List<dynamic>();

        foreach (var goal in goals)
        {
            var totalSpent = await _context.Transactions
                .Where(t => t.UserId == userId &&
                           t.CategoryId == goal.CategoryId &&
                           t.Type == Core.Enums.TransactionType.Expense &&
                           t.Date >= startDate &&
                           t.Date <= endDate)
                .SumAsync(t => t.Amount);

            var percentage = goal.TargetAmount > 0 ? (totalSpent / goal.TargetAmount) * 100 : 0;
            var status = percentage switch
            {
                < 80 => "No Caminho",
                >= 80 and < 100 => "Atenção",
                _ => "Excedido"
            };

            goalsWithProgress.Add(new
            {
                CategoryName = goal.Category.Name,
                TargetAmount = goal.TargetAmount,
                CurrentAmount = totalSpent,
                Percentage = percentage,
                Status = status
            });
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().Element(c => PdfHelper.CreateHeader(
                    c,
                    "Relatório de Metas",
                    $"Referência: {CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetMonthName(currentMonth)}/{currentYear}"
                ));

                page.Content().Column(column =>
                {
                    column.Item().PaddingBottom(15).Text("🎯 Status das Metas").FontSize(16).SemiBold();

                    foreach (var goal in goalsWithProgress)
                    {
                        var statusColor = goal.Status switch
                        {
                            "No Caminho" => Colors.Green.Medium,
                            "Atenção" => Colors.Orange.Medium,
                            _ => Colors.Red.Medium
                        };

                        column.Item().PaddingBottom(15).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                        {
                            string categoryName = goal.CategoryName;
                            string statusText = goal.Status;
                            decimal currentAmount = goal.CurrentAmount;
                            decimal targetAmount = goal.TargetAmount;
                            double percentage = goal.Percentage;

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text(categoryName).FontSize(14).SemiBold();
                                row.ConstantItem(100).AlignRight().Text(statusText).FontSize(12).FontColor(statusColor).SemiBold();
                            });

                            col.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text($"Gasto: {PdfHelper.FormatCurrency(currentAmount)}").FontSize(12);
                                row.RelativeItem().AlignRight().Text($"Meta: {PdfHelper.FormatCurrency(targetAmount)}").FontSize(12);
                            });

                            col.Item().PaddingTop(5).Column(progressCol =>
                            {
                                progressCol.Item().Height(20).Background(Colors.Grey.Lighten2);
                                
                                var progressWidth = Math.Min(goal.Percentage, 100);
                                if (progressWidth > 0)
                                {
                                    progressCol.Item().TranslateY(-20).Width((float)progressWidth).Height(20).Background(statusColor);
                                }
                            });

                            col.Item().PaddingTop(2).Text($"{goal.Percentage:F1}%").FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                    }

                    if (!goalsWithProgress.Any())
                    {
                        column.Item().PaddingVertical(20).AlignCenter().Text("Nenhuma meta cadastrada para este período.").FontSize(14).Italic().FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Footer().Element(PdfHelper.CreateFooter);
            });
        });

        return document.GeneratePdf();
    }

    #endregion
}