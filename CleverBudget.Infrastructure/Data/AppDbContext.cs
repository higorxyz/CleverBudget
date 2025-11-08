using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CleverBudget.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Goal> Goals { get; set; }
    public DbSet<RecurringTransaction> RecurringTransactions { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<FinancialInsightRecord> FinancialInsights { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            entity.Property(t => t.Description).HasMaxLength(500).IsRequired();
            entity.Property(t => t.Date).IsRequired();
            entity.Property(t => t.CreatedAt).IsRequired();
            
            entity.HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Icon).HasMaxLength(50);
            entity.Property(c => c.Color).HasMaxLength(20);
            entity.Property(c => c.IsDefault).HasConversion<bool>();
            entity.Property(c => c.CreatedAt).IsRequired();
            entity.Property(c => c.Kind)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(CategoryKind.Essential);
            entity.Property(c => c.Segment)
                .HasMaxLength(120);
            entity.Property(c => c.Tags)
                .HasColumnType("text")
                .HasDefaultValue("[]");

            entity.HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.TargetAmount).HasColumnType("decimal(18,2)");
            entity.Property(g => g.CreatedAt).IsRequired();

            entity.HasOne(g => g.User)
                .WithMany(u => u.Goals)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(g => g.Category)
                .WithMany(c => c.Goals)
                .HasForeignKey(g => g.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecurringTransaction>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Amount).HasColumnType("decimal(18,2)");
            entity.Property(r => r.Description).HasMaxLength(500).IsRequired();
            entity.Property(r => r.IsActive).HasConversion<bool>();
            entity.Property(r => r.StartDate).IsRequired();
            entity.Property(r => r.CreatedAt).IsRequired();

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Category)
                .WithMany()
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => new { r.UserId, r.IsActive });
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Amount).HasColumnType("decimal(18,2)");
            entity.Property(b => b.Month).IsRequired();
            entity.Property(b => b.Year).IsRequired();
            entity.Property(b => b.AlertAt50Percent).HasConversion<bool>();
            entity.Property(b => b.AlertAt80Percent).HasConversion<bool>();
            entity.Property(b => b.AlertAt100Percent).HasConversion<bool>();
            entity.Property(b => b.Alert50Sent).HasConversion<bool>();
            entity.Property(b => b.Alert80Sent).HasConversion<bool>();
            entity.Property(b => b.Alert100Sent).HasConversion<bool>();
            entity.Property(b => b.CreatedAt).IsRequired();

            entity.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(b => b.Category)
                .WithMany()
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(b => new { b.UserId, b.CategoryId, b.Month, b.Year })
                .IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<FinancialInsightRecord>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Title).HasMaxLength(200).IsRequired();
            entity.Property(f => f.Summary).HasMaxLength(1000);
            entity.Property(f => f.Recommendation).HasMaxLength(1500);
            entity.Property(f => f.DataPointsJson).HasColumnType("text").IsRequired();
            entity.Property(f => f.ImpactAmount).HasColumnType("decimal(18,2)");
            entity.Property(f => f.BenchmarkAmount).HasColumnType("decimal(18,2)");
            entity.Property(f => f.Category).HasConversion<string>().HasMaxLength(50);
            entity.Property(f => f.Severity).HasConversion<string>().HasMaxLength(20);
            entity.Property(f => f.IncludeIncomeInsights).HasConversion<bool>();
            entity.Property(f => f.IncludeExpenseInsights).HasConversion<bool>();

            entity.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(f => new { f.UserId, f.GeneratedAt });
            entity.HasIndex(f => new { f.UserId, f.Category, f.Severity });
        });
    }
}