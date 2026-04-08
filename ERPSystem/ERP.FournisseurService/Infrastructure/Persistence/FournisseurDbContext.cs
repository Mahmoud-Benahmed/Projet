using ERP.FournisseurService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.FournisseurService.Infrastructure.Persistence;

public sealed class FournisseurDbContext(DbContextOptions<FournisseurDbContext> options) : DbContext(options)
{
    // Entities
    public DbSet<Fournisseur> Fournisseurs => Set<Fournisseur>();


    protected override void OnModelCreating(ModelBuilder m) =>
        m.ApplyConfigurationsFromAssembly(typeof(FournisseurDbContext).Assembly);
}


// ── Fournisseur ───────────────────────────────────────────────────────────────
internal sealed class FournisseurConfiguration : IEntityTypeConfiguration<Fournisseur>
{
    public void Configure(EntityTypeBuilder<Fournisseur> b)
    {
        b.ToTable("Fournisseurs");
        b.HasKey(f => f.Id);
        b.Property(f => f.Name).IsRequired().HasMaxLength(200);
        b.Property(f => f.Address).IsRequired().HasMaxLength(500);
        b.Property(f => f.Phone).IsRequired().HasMaxLength(50);
        b.Property(f => f.Email).HasMaxLength(200);
        b.Property(f => f.TaxNumber).IsRequired().HasMaxLength(50);
        b.Property(f => f.RIB).IsRequired().HasMaxLength(50);
        b.Property(f => f.IsDeleted).IsRequired();
        b.Property(f => f.IsBlocked).IsRequired();
        b.Property(f => f.CreatedAt).IsRequired();
        b.Property(f => f.UpdatedAt)
                 .IsConcurrencyToken(false)
                 .ValueGeneratedNever();

        b.HasIndex(f => f.TaxNumber)
         .IsUnique()
         .HasDatabaseName("IX_Fournisseurs_TaxNumber")
         .HasFilter("[IsDeleted] = 0");


        b.HasQueryFilter(f => !f.IsDeleted);
    }
}
