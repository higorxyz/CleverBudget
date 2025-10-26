using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Data;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
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
}