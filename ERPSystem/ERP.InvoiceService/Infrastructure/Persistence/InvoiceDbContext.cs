using Microsoft.EntityFrameworkCore;
using InvoiceService.Domain;

namespace ERP.InvoiceService.Infrastructure.Persistence
{

    public class InvoiceDbContext : DbContext
    {
      
        public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options)
            : base(options) { }
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ════════════════════════════════════════════════════════════════════
            // INVOICE ENTITY CONFIGURATION
            // ════════════════════════════════════════════════════════════════════

            modelBuilder.Entity<Invoice>(entity =>
            {
                // ──── TABLE ────
                entity.ToTable("Invoices");

            
                entity.HasKey(i => i.Id);


                entity.Property(i => i.InvoiceNumber)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(i => i.ClientFullName)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(i => i.ClientAddress)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(i => i.AdditionalNotes)
                      .HasMaxLength(1000);

            
                entity.Property(i => i.TotalHT)
                      .HasColumnType("decimal(18,4)");

                entity.Property(i => i.TotalTVA)
                      .HasColumnType("decimal(18,4)");

                entity.Property(i => i.TotalTTC)
                      .HasColumnType("decimal(18,4)");

                entity.Property(i => i.Status)
                      .HasConversion<string>()  
                      .HasMaxLength(20);
                entity.HasIndex(i => i.InvoiceNumber).IsUnique();

                entity.HasMany(i => i.Items)
                      .WithOne()
                      .HasForeignKey(ii => ii.InvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);


                entity.HasQueryFilter(i => !i.IsDeleted);
            });


            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                // ──── TABLE ────
                entity.ToTable("InvoiceItems");

                // ──── PRIMARY KEY ────
                entity.HasKey(ii => ii.Id);

                // ──── COLUMNS ────

                entity.Property(ii => ii.ArticleName)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(ii => ii.ArticleBarCode)
                      .IsRequired()
                      .HasMaxLength(100);

                // Decimal columns with precision
                entity.Property(ii => ii.UniPriceHT)
                      .HasColumnType("decimal(18,4)");

                // Tax rate: decimal(5,4) allows 0.0000 to 9.9999 (covers 0% to 999.99%)
                entity.Property(ii => ii.TaxRate)
                      .HasColumnType("decimal(5,4)");

                entity.Property(ii => ii.TotalHT)
                      .HasColumnType("decimal(18,4)");

                entity.Property(ii => ii.TotalTTC)
                      .HasColumnType("decimal(18,4)");
            });
        }
    }
}