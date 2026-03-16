using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Data;

public class AccountDbContext : DbContext
{
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;

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

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Operation)
                .HasConversion(
                    operation => operation.ToString().ToLowerInvariant(),
                    operation => Enum.Parse<TransactionOperation>(operation, ignoreCase: true))
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.AccountIdentification).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.ReferenceId).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Status)
                .HasConversion(
                    status => status.ToString().ToLowerInvariant(),
                    status => Enum.Parse<TransactionStatus>(status, ignoreCase: true))
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.HasIndex(e => e.ReferenceId).IsUnique();
            entity.HasIndex(e => e.AccountIdentification);

            entity.HasOne<Account>()
                .WithMany()
                .HasForeignKey(e => e.AccountIdentification)
                .HasPrincipalKey(a => a.Identification)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable("Transaction");
        });
    }
}

