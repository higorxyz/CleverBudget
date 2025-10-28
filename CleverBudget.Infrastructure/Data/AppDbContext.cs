using CleverBudget.Core.Entities;
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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var isSqlite = Database.IsSqlite();

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            entity.Property(t => t.Description).HasMaxLength(500);
            
            if (isSqlite)
            {
                entity.Property(t => t.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.Property(t => t.Date).HasColumnType("TEXT");
            }
            else
            {
                entity.Property(t => t.CreatedAt).HasDefaultValueSql("NOW()");
            }
            
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
            
            if (isSqlite)
            {
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("datetime('now')");
            }
            else
            {
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");
            }

            entity.HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.TargetAmount).HasColumnType("decimal(18,2)");
            
            if (isSqlite)
            {
                entity.Property(g => g.CreatedAt).HasDefaultValueSql("datetime('now')");
            }
            else
            {
                entity.Property(g => g.CreatedAt).HasDefaultValueSql("NOW()");
            }

            entity.HasOne(g => g.User)
                .WithMany(u => u.Goals)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(g => g.Category)
                .WithMany(c => c.Goals)
                .HasForeignKey(g => g.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            if (isSqlite)
            {
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("datetime('now')");
            }
            else
            {
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
            }
        });
    }
}