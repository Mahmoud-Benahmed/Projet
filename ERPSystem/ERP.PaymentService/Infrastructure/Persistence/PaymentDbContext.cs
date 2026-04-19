using ERP.PaymentService.Domain.Entities;
using ERP.PaymentService.Domain.LocalCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.PaymentService.Infrastructure.Persistence
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
            : base(options) { }

        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<LateFeePolicy> LateFeePolicies => Set<LateFeePolicy>();
        public DbSet<Invoice> InvoiceCache => Set<Invoice>();
        public DbSet<Client> ClientCache => Set<Client>();

        protected override void OnModelCreating(ModelBuilder m) =>
            m.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PAYMENT CONFIGURATION
    // ═══════════════════════════════════════════════════════════════════════════

    internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> entity)
        {
            entity.ToTable("Payments");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.InvoiceId).IsRequired();
            entity.Property(p => p.ClientId).IsRequired();

            entity.Property(p => p.Amount)
                  .HasColumnType("decimal(18,4)")
                  .IsRequired();

            entity.Property(p => p.PaymentDate).IsRequired();

            entity.Property(p => p.Method)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(p => p.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(p => p.LateFeeApplied)
                  .HasColumnType("decimal(18,4)")
                  .HasDefaultValue(0);

            entity.Property(p => p.IsDeleted).IsRequired();
            entity.Property(p => p.CreatedAt).IsRequired();
            entity.Property(p => p.UpdatedAt).IsRequired();

            entity.HasIndex(p => p.InvoiceId);
            entity.HasIndex(p => p.ClientId);
            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.IsDeleted);

            entity.HasQueryFilter(p => !p.IsDeleted);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LATE FEE POLICY CONFIGURATION
    // ═══════════════════════════════════════════════════════════════════════════

    internal sealed class LateFeePolicyConfiguration : IEntityTypeConfiguration<LateFeePolicy>
    {
        public void Configure(EntityTypeBuilder<LateFeePolicy> entity)
        {
            entity.ToTable("LateFeePolicies");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.FeePercentage)
                  .HasColumnType("decimal(18,4)")
                  .IsRequired();

            entity.Property(p => p.FeeType)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(p => p.GracePeriodDays).IsRequired();
            entity.Property(p => p.IsActive).IsRequired();
            entity.Property(p => p.CreatedAt).IsRequired();
            entity.Property(p => p.UpdatedAt).IsRequired();

            entity.HasIndex(p => p.IsActive);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CACHE CONFIGURATIONS
    // ═══════════════════════════════════════════════════════════════════════════

    internal sealed class InvoiceCacheConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> entity)
        {
            entity.ToTable("InvoiceCache");

            entity.HasKey(i => i.InvoiceId);
            entity.Property(i => i.InvoiceId).ValueGeneratedNever();

            entity.Property(i => i.ClientId).IsRequired();

            entity.Property(i => i.TotalTTC)
                  .HasColumnType("decimal(18,4)")
                  .IsRequired();

            entity.Property(i => i.TotalPaid)
                  .HasColumnType("decimal(18,4)")
                  .HasDefaultValue(0);

            entity.Property(i => i.DueDate).IsRequired();
            entity.Property(i => i.InvoiceDate).IsRequired();

            entity.Property(i => i.Status)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.Property(i => i.LateFeeApplied).HasDefaultValue(false);

            entity.Property(i => i.LateFeeAmount)
                  .HasColumnType("decimal(18,4)")
                  .HasDefaultValue(0);

            entity.HasIndex(i => i.ClientId);
            entity.HasIndex(i => i.Status);
        }
    }

    internal sealed class ClientCacheConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> entity)
        {
            entity.ToTable("ClientCache");

            entity.HasKey(c => c.ClientId);
            entity.Property(c => c.ClientId).ValueGeneratedNever();

            entity.Property(c => c.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(c => c.DelaiRetour).IsRequired();
            entity.Property(c => c.IsBlocked).IsRequired();
            entity.Property(c => c.IsDeleted).IsRequired();

            entity.HasIndex(c => c.IsBlocked);
            entity.HasIndex(c => c.IsDeleted);
        }
    }
}
