using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Enums;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Helpers;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CleverBudget.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly AppDbContext _context;
    private readonly IBudgetService _budgetService;

    public ExportService(AppDbContext context, IBudgetService budgetService)
    {
        _context = context;
        _budgetService = budgetService;
    }

    #region CSV Exports (já existentes)

    public async Task<byte[]> ExportTransactionsToCsvAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, ExportRequestOptions? options = null)
    {
        var resolved = options ?? new ExportRequestOptions();
        var culture = resolved.Culture;
        var start = (startDate ?? DateTime.Now.AddMonths(-12)).Date;
        var end = (endDate ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= start && t.Date <= end)
            .OrderByDescending(t => t.Date)
            .Select(t => new
            {
                t.Date,
                t.Type,
                t.Amount,
                CategoryName = t.Category.Name,
                t.Description
            })
            .ToListAsync();

        if (resolved.Variant == ExportVariant.Summary)
        {
            var summary = transactions
                .GroupBy(t => t.Type)
                .Select(g => new
                {
                    Tipo = g.Key.ToString(),
                    Quantidade = g.Count(),
                    ValorTotal = g.Sum(x => x.Amount).ToString("C2", culture)
                })
                .ToList();

            return GenerateCsv(summary, resolved, culture);
        }

        var detailed = transactions.Select(t => new
        {
            Data = t.Date.ToString("d", culture),
            Tipo = t.Type.ToString(),
            Valor = t.Amount.ToString("C2", culture),
            Categoria = t.CategoryName,
            Descricao = t.Description
        });

        return GenerateCsv(detailed, resolved, culture);
    }

    public async Task<byte[]> ExportCategoriesToCsvAsync(string userId, ExportRequestOptions? options = null)
    {
        var resolved = options ?? new ExportRequestOptions();
        var culture = resolved.Culture;

        var categories = await _context.Categories
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Name,
                c.Icon,
                c.Color,
                c.IsDefault,
                c.Kind,
                c.Segment,
                c.Tags,
                c.CreatedAt
            })
            .ToListAsync();

        if (resolved.Variant == ExportVariant.Summary)
        {
            var summary = categories
                .GroupBy(c => c.Kind)
                .Select(g => new
                {
                    Tipo = g.Key.ToString(),
                    Quantidade = g.Count()
                })
                .ToList();

            return GenerateCsv(summary, resolved, culture);
        }

        var detailed = categories.Select(c => new
        {
            Nome = c.Name,
            Icone = c.Icon ?? string.Empty,
            Cor = c.Color ?? string.Empty,
            Padrao = c.IsDefault ? "Sim" : "Não",
            Tipo = c.Kind.ToString(),
            Segmento = c.Segment ?? string.Empty,
            Tags = RenderTags(c.Tags),
            CriadaEm = c.CreatedAt.ToString("d", culture)
        });

        return GenerateCsv(detailed, resolved, culture);
    }

    public async Task<byte[]> ExportGoalsToCsvAsync(string userId, int? month = null, int? year = null, ExportRequestOptions? options = null)
    {
        var resolved = options ?? new ExportRequestOptions();
        var culture = resolved.Culture;

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
                g.Category.Name,
                g.TargetAmount,
                g.Month,
                g.Year,
                g.CreatedAt
            })
            .ToListAsync();

        if (resolved.Variant == ExportVariant.Summary)
        {
            var summary = goals
                .GroupBy(g => new { g.Year, g.Month })
                .Select(g => new
                {
                    Mes = g.Key.Month,
                    Ano = g.Key.Year,
                    Metas = g.Count(),
                    ValorTotal = g.Sum(x => x.TargetAmount).ToString("C2", culture)
                })
                .OrderByDescending(g => g.Ano)
                .ThenByDescending(g => g.Mes)
                .ToList();

            return GenerateCsv(summary, resolved, culture);
        }

        var detailed = goals.Select(g => new
        {
            Mes = g.Month,
            Ano = g.Year,
            Categoria = g.Name,
            MetaValor = g.TargetAmount.ToString("C2", culture),
            CriadaEm = g.CreatedAt.ToString("d", culture)
        });

        return GenerateCsv(detailed, resolved, culture);
    }

    public async Task<byte[]> ExportBudgetOverviewToCsvAsync(string userId, int? year = null, int? month = null, ExportRequestOptions? options = null)
    {
        var resolved = options ?? new ExportRequestOptions();
        var culture = resolved.Culture;
        var overview = await _budgetService.GetOverviewAsync(userId, year, month);

        if (resolved.Variant == ExportVariant.Summary)
        {
            var summary = new[]
            {
                new
                {
                    Referencia = $"{overview.Month}/{overview.Year}",
                    TotalPlanejado = overview.TotalBudget.ToString("C2", culture),
                    TotalGasto = overview.TotalSpent.ToString("C2", culture),
                    Saldo = overview.Remaining.ToString("C2", culture),
                    Utilizacao = $"{overview.PercentageUsed:F2}%",
                    Recomendacao = overview.Recommendation
                }
            };

            return GenerateCsv(summary, resolved, culture);
        }

        var detailed = overview.Categories
            .Select(c => new
            {
                Categoria = c.CategoryName,
                ValorPlanejado = c.Amount.ToString("C2", culture),
                ValorGasto = c.Spent.ToString("C2", culture),
                Saldo = c.Remaining.ToString("C2", culture),
                PercentualUtilizado = $"{c.PercentageUsed:F2}%",
                Status = c.Status,
                GastoProjetado = c.ProjectedSpend.ToString("C2", culture),
                VariacaoProjetada = c.ProjectedVariance.ToString("C2", culture),
                OrcamentoSugerido = c.SuggestedBudget.ToString("C2", culture),
                VariacaoOrcamento = c.BudgetVariance.ToString("C2", culture),
                ReallocacaoPotencial = c.PotentialReallocation.ToString("C2", culture),
                Transacoes = c.TransactionsCount,
                UltimaTransacao = c.LastTransactionDate?.ToString("d", culture) ?? "-",
                Recomendacao = c.Recommendation
            })
            .Prepend(new
            {
                Categoria = "TOTAL",
                ValorPlanejado = overview.TotalBudget.ToString("C2", culture),
                ValorGasto = overview.TotalSpent.ToString("C2", culture),
                Saldo = overview.Remaining.ToString("C2", culture),
                PercentualUtilizado = $"{overview.PercentageUsed:F2}%",
                Status = "Resumo",
                GastoProjetado = overview.TotalSpent.ToString("C2", culture),
                VariacaoProjetada = (overview.TotalSpent - overview.TotalBudget).ToString("C2", culture),
                OrcamentoSugerido = overview.SuggestedReallocation.ToString("C2", culture),
                VariacaoOrcamento = (overview.TotalBudget - overview.TotalSpent).ToString("C2", culture),
                ReallocacaoPotencial = overview.SuggestedReallocation.ToString("C2", culture),
                Transacoes = overview.Categories.Sum(c => c.TransactionsCount),
                UltimaTransacao = "-",
                Recomendacao = overview.Recommendation
            });

        return GenerateCsv(detailed, resolved, culture);
    }

    private static string RenderTags(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        try
        {
            var tags = System.Text.Json.JsonSerializer.Deserialize<string[]>(raw);
            if (tags == null || tags.Length == 0)
            {
                return string.Empty;
            }

            return string.Join(", ", tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()));
        }
        catch
        {
            return raw;
        }
    }

    private byte[] GenerateCsv<T>(IEnumerable<T> records, ExportRequestOptions options, CultureInfo culture)
    {
        using var memoryStream = new MemoryStream();
        using var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(culture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true
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
    public async Task<byte[]> ExportTransactionsToPdfAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, ExportRequestOptions? options = null)
    {
        var resolved = options ?? new ExportRequestOptions();
        var culture = resolved.Culture;
        var start = (startDate ?? DateTime.Now.AddMonths(-1)).Date;
        var end = (endDate ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= start && t.Date <= end)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        var totalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var balance = totalIncome - totalExpense;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().Element(c => PdfHelper.CreateHeader(
                    c,
                    "Relatório de Transações",
                    $"Período: {start.ToString("d", culture)} a {end.ToString("d", culture)}"));

                page.Content().Column(column =>
                {
                    column.Item().PaddingBottom(15).Text("Resumo geral").FontSize(16).SemiBold();

                    column.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Green.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Receitas").FontSize(12).SemiBold();
                            col.Item().Text(PdfHelper.FormatCurrency(totalIncome, culture)).FontSize(16).SemiBold().FontColor(Colors.Green.Darken2);
                        });

                        row.RelativeItem().Background(Colors.Red.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Despesas").FontSize(12).SemiBold();
                            col.Item().Text(PdfHelper.FormatCurrency(totalExpense, culture)).FontSize(16).SemiBold().FontColor(Colors.Red.Darken2);
                        });

                        row.RelativeItem().Background(Colors.Blue.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Saldo").FontSize(12).SemiBold();
                            col.Item().Text(PdfHelper.FormatCurrency(balance, culture)).FontSize(16).SemiBold().FontColor(balance >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                        });
                    });

                    if (resolved.Variant == ExportVariant.Detailed)
                    {
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
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(transaction.Date.ToString("d", culture));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(transaction.Type == TransactionType.Income ? "Receita" : "Despesa");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(transaction.Category.Name);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(transaction.Description);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                                    .Text(PdfHelper.FormatCurrency(transaction.Amount, culture))
                                    .FontColor(transaction.Type == TransactionType.Income ? Colors.Green.Darken2 : Colors.Red.Darken2);
                            }
                        });

                        if (!transactions.Any())
                        {
                            column.Item().PaddingVertical(20).AlignCenter().Text("Nenhuma transação encontrada no período.").FontSize(14).Italic().FontColor(Colors.Grey.Darken1);
                        }
                    }
                    else
                    {
                        column.Item().PaddingTop(10).Text($"Transações registradas: {transactions.Count}").FontSize(12);
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
    public async Task<byte[]> ExportFinancialReportToPdfAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, ExportRequestOptions? options = null)
    {
        var resolved = options ?? new ExportRequestOptions();
        var culture = resolved.Culture;
        var start = (startDate ?? DateTime.Now.AddMonths(-1)).Date;
        var end = (endDate ?? DateTime.Now).Date.AddDays(1).AddTicks(-1);

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= start && t.Date <= end)
            .ToListAsync();

        var totalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var balance = totalIncome - totalExpense;

        var expensesByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense)
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
                    $"Período: {start.ToString("d", culture)} a {end.ToString("d", culture)}"));

                page.Content().Column(column =>
                {
                    column.Item().PaddingBottom(15).Text("Resumo Geral").FontSize(16).SemiBold();

                    column.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Green.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Total de Receitas").FontSize(12);
                            col.Item().Text(PdfHelper.FormatCurrency(totalIncome, culture)).FontSize(18).SemiBold().FontColor(Colors.Green.Darken2);
                        });

                        row.RelativeItem().Background(Colors.Red.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Total de Despesas").FontSize(12);
                            col.Item().Text(PdfHelper.FormatCurrency(totalExpense, culture)).FontSize(18).SemiBold().FontColor(Colors.Red.Darken2);
                        });

                        row.RelativeItem().Background(Colors.Blue.Lighten3).Padding(10).Column(col =>
                        {
                            col.Item().Text("Saldo Final").FontSize(12);
                            col.Item().Text(PdfHelper.FormatCurrency(balance, culture)).FontSize(18).SemiBold().FontColor(balance >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                        });
                    });

                    if (resolved.Variant == ExportVariant.Detailed)
                    {
                        column.Item().PaddingVertical(10).Text("Despesas por Categoria").FontSize(16).SemiBold();

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
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Count.ToString(culture));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(PdfHelper.FormatCurrency(item.Total, culture)).FontColor(Colors.Red.Darken2);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{item.Percentage:F1}%");
                            }
                        });

                        if (!expensesByCategory.Any())
                        {
                            column.Item().PaddingVertical(10).AlignCenter().Text("Nenhuma despesa registrada no período.").Italic().FontColor(Colors.Grey.Darken1);
                        }
                    }
                    else
                    {
                        column.Item().PaddingTop(10).Text($"Categorias com despesas: {expensesByCategory.Count}").FontSize(12);
                    }
                });

                page.Footer().Element(PdfHelper.CreateFooter);
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportBudgetOverviewToPdfAsync(string userId, int? year = null, int? month = null, ExportRequestOptions? options = null)
    {
        var resolved = options ?? new ExportRequestOptions();
        var culture = resolved.Culture;
        var overview = await _budgetService.GetOverviewAsync(userId, year, month);
        var titlePeriod = new DateTime(overview.Year, overview.Month, 1);
        var monthLabel = culture.DateTimeFormat.GetMonthName(titlePeriod.Month);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().Element(c => PdfHelper.CreateHeader(
                    c,
                    "Visão Consolidada de Orçamentos",
                    $"Referência: {monthLabel}/{titlePeriod.Year}"));

                page.Content().Column(column =>
                {
                    column.Item().PaddingBottom(15).Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Blue.Lighten4).Padding(12).Column(col =>
                        {
                            col.Item().Text("Total Planejado").FontSize(12).SemiBold();
                            col.Item().Text(PdfHelper.FormatCurrency(overview.TotalBudget, culture)).FontSize(18).SemiBold().FontColor(Colors.Blue.Darken2);
                        });

                        row.RelativeItem().Background(Colors.Red.Lighten4).Padding(12).Column(col =>
                        {
                            col.Item().Text("Total Gasto").FontSize(12).SemiBold();
                            col.Item().Text(PdfHelper.FormatCurrency(overview.TotalSpent, culture)).FontSize(18).SemiBold().FontColor(Colors.Red.Darken2);
                        });

                        row.RelativeItem().Background(Colors.Green.Lighten4).Padding(12).Column(col =>
                        {
                            col.Item().Text("Saldo Restante").FontSize(12).SemiBold();
                            col.Item().Text(PdfHelper.FormatCurrency(overview.Remaining, culture)).FontSize(18).SemiBold().FontColor(Colors.Green.Darken2);
                        });
                    });

                    column.Item().PaddingBottom(10).Text($"Utilização: {overview.PercentageUsed:F2}%").FontSize(12).FontColor(Colors.Blue.Darken2);

                    if (!string.IsNullOrWhiteSpace(overview.Recommendation))
                    {
                        column.Item().Background(Colors.Grey.Lighten4).BorderColor(Colors.Grey.Lighten2).Border(1).Padding(10).Column(col =>
                        {
                            col.Item().Text("Recomendação Geral").FontSize(12).SemiBold();
                            col.Item().Text(overview.Recommendation).FontSize(11);
                        });
                    }

                    if (resolved.Variant == ExportVariant.Detailed)
                    {
                        column.Item().PaddingTop(15).Text("Categorias").FontSize(14).SemiBold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Categoria").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Planejado").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Gasto").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Saldo").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("% Utilizado").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Recomendação").SemiBold();
                            });

                            foreach (var category in overview.Categories)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(category.CategoryName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(PdfHelper.FormatCurrency(category.Amount, culture));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(PdfHelper.FormatCurrency(category.Spent, culture));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(PdfHelper.FormatCurrency(category.Remaining, culture));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{category.PercentageUsed:F2}%");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(category.Recommendation).FontSize(10);
                            }
                        });

                        if (overview.AtRisk.Any())
                        {
                            column.Item().PaddingTop(15).Text("Categorias em Risco").FontSize(13).SemiBold().FontColor(Colors.Red.Darken2);
                            column.Item().Column(listCol =>
                            {
                                foreach (var risk in overview.AtRisk)
                                {
                                    listCol.Item().Text($"• {risk.CategoryName}: excedente projetado de {PdfHelper.FormatCurrency(risk.ProjectedVariance, culture)}");
                                }
                            });
                        }

                        if (overview.Comfortable.Any())
                        {
                            column.Item().PaddingTop(10).Text("Categorias Confortáveis").FontSize(13).SemiBold().FontColor(Colors.Green.Darken2);
                            column.Item().Column(listCol =>
                            {
                                foreach (var comfy in overview.Comfortable)
                                {
                                    listCol.Item().Text($"• {comfy.CategoryName}: potencial de realocação {PdfHelper.FormatCurrency(comfy.PotentialReallocation, culture)}");
                                }
                            });
                        }
                    }
                    else
                    {
                        column.Item().PaddingTop(15).Text("Resumo de Categorias").FontSize(14).SemiBold();
                        column.Item().Text($"Categorias acompanhadas: {overview.Categories.Count}").FontSize(12);
                        column.Item().Text($"Categorias em risco: {overview.AtRisk.Count}").FontSize(12).FontColor(Colors.Red.Darken2);
                        column.Item().Text($"Categorias confortáveis: {overview.Comfortable.Count}").FontSize(12).FontColor(Colors.Green.Darken2);
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
    public async Task<byte[]> ExportGoalsReportToPdfAsync(string userId, int? month = null, int? year = null, ExportRequestOptions? options = null)
    {
        var resolved = options ?? new ExportRequestOptions();
        var culture = resolved.Culture;
        var currentMonth = month ?? DateTime.Now.Month;
        var currentYear = year ?? DateTime.Now.Year;

        var goals = await _context.Goals
            .Include(g => g.Category)
            .Where(g => g.UserId == userId && g.Month == currentMonth && g.Year == currentYear)
            .ToListAsync();

        var startDate = new DateTime(currentYear, currentMonth, 1);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var goalsWithProgress = new List<(string CategoryName, decimal TargetAmount, decimal CurrentAmount, double Percentage, string Status)>();

        foreach (var goal in goals)
        {
            var totalSpent = await _context.Transactions
                .Where(t => t.UserId == userId &&
                           t.CategoryId == goal.CategoryId &&
                           t.Type == Core.Enums.TransactionType.Expense &&
                           t.Date >= startDate &&
                           t.Date <= endDate)
                .SumAsync(t => t.Amount);

            var percentage = goal.TargetAmount > 0 ? (double)(totalSpent / goal.TargetAmount) * 100d : 0d;
            var status = percentage switch
            {
                < 80 => "No Caminho",
                >= 80 and < 100 => "Atenção",
                _ => "Excedido"
            };

            goalsWithProgress.Add((goal.Category.Name, goal.TargetAmount, totalSpent, percentage, status));
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
                    $"Referência: {culture.DateTimeFormat.GetMonthName(currentMonth)}/{currentYear}"));

                page.Content().Column(column =>
                {
                    column.Item().PaddingBottom(15).Text("Status das Metas").FontSize(16).SemiBold();

                    if (goalsWithProgress.Any())
                    {
                        if (resolved.Variant == ExportVariant.Detailed)
                        {
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
                                    col.Item().Text(goal.CategoryName).FontSize(14).SemiBold();
                                    col.Item().Text($"Status: {goal.Status}").FontSize(12).FontColor(statusColor);
                                    col.Item().Text($"Meta: {PdfHelper.FormatCurrency(goal.TargetAmount, culture)}").FontSize(12);
                                    col.Item().Text($"Realizado: {PdfHelper.FormatCurrency(goal.CurrentAmount, culture)}").FontSize(12);
                                    col.Item().Text($"Progresso: {goal.Percentage:F2}%").FontSize(12);

                                    if (goal.Status == "Atenção")
                                    {
                                        col.Item().PaddingTop(5).Text("Recomendação: ajuste seu orçamento para evitar ultrapassar a meta.").FontSize(11).FontColor(Colors.Orange.Darken2);
                                    }
                                    else if (goal.Status == "Excedido")
                                    {
                                        col.Item().PaddingTop(5).Text("Status crítico: considere rever suas metas e planejar realocações.").FontSize(11).FontColor(Colors.Red.Darken2);
                                    }
                                });
                            }
                        }
                        else
                        {
                            var attentionCount = goalsWithProgress.Count(g => g.Status == "Atenção");
                            var exceededCount = goalsWithProgress.Count(g => g.Status == "Excedido");
                            var onTrackCount = goalsWithProgress.Count(g => g.Status == "No Caminho");

                            column.Item().Text($"Metas monitoradas: {goalsWithProgress.Count}").FontSize(12);
                            column.Item().Text($"No caminho: {onTrackCount}").FontSize(12).FontColor(Colors.Green.Darken2);
                            column.Item().Text($"Em atenção: {attentionCount}").FontSize(12).FontColor(Colors.Orange.Darken2);
                            column.Item().Text($"Excedidas: {exceededCount}").FontSize(12).FontColor(Colors.Red.Darken2);
                        }
                    }
                    else
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