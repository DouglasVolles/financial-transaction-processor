using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Data;

public class AccountDbContext : DbContext
{
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.Identification).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AvailableBalance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ReservedBalance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreditLimit).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AccountStatus).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(e => e.Identification).IsUnique();
            entity.ToTable("Accounts");
        });
    }
}

