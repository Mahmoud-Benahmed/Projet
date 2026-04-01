using ERP.StockService.Domain;
using ERP.StockService.Domain.Entre;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.StockService.Infrastructure.Persistence;

public sealed class StockDbContext(DbContextOptions<StockDbContext> options) : DbContext(options)
{
    public DbSet<Fournisseur> Fournisseurs => Set<Fournisseur>();
    public DbSet<BonEntre> BonEntres => Set<BonEntre>();
    public DbSet<BonSortie> BonSorties => Set<BonSortie>();
    public DbSet<BonRetour> BonRetours => Set<BonRetour>();
    public DbSet<LigneEntre> LigneEntres => Set<LigneEntre>();
    public DbSet<LigneSortie> LigneSorties => Set<LigneSortie>();
    public DbSet<LigneRetour> LigneRetours => Set<LigneRetour>();

    protected override void OnModelCreating(ModelBuilder m) =>
        m.ApplyConfigurationsFromAssembly(typeof(StockDbContext).Assembly);

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified && e.Entity is PieceStock))
        {
            var prop = entry.Property("UpdatedAt");

            // ✅ Tell EF: "the original DB value is whatever is currently there"
            //    so the WHERE clause uses the current DB value, not null
            prop.OriginalValue = prop.CurrentValue;

            // ✅ Then set the new value we want to save
            prop.CurrentValue = now;
        }

        return await base.SaveChangesAsync(ct);
    }

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
                 .ValueGeneratedNever()
                 .UsePropertyAccessMode(PropertyAccessMode.Property);

        b.HasIndex(f => f.TaxNumber)
         .IsUnique()
         .HasDatabaseName("IX_Fournisseurs_TaxNumber")
         .HasFilter("[IsDeleted] = 0");

        b.HasQueryFilter(f => !f.IsDeleted);
    }
}

// ── BonEntre ──────────────────────────────────────────────────────────────────
internal sealed class BonEntreConfiguration : IEntityTypeConfiguration<BonEntre>
{
    public void Configure(EntityTypeBuilder<BonEntre> b)
    {
        b.ToTable("BonEntres");
        b.HasKey(x => x.Id);
        b.Property(x => x.Numero).IsRequired().HasMaxLength(50);
        b.Property(x => x.Observation).HasMaxLength(1000);
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt)
         .IsConcurrencyToken(false)
         .ValueGeneratedNever()
        .UsePropertyAccessMode(PropertyAccessMode.Property);

        b.HasIndex(x => x.Numero)
         .IsUnique()
         .HasDatabaseName("IX_BonEntres_Numero")
         .HasFilter("[IsDeleted] = 0");

        b.HasOne(x => x.Fournisseur)
         .WithMany()
         .HasForeignKey(x => x.FournisseurId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Lignes)
         .WithOne(l => l.BonEntre)
         .HasForeignKey(l => l.BonEntreId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

// ── LigneEntre ────────────────────────────────────────────────────────────────
internal sealed class LigneEntreConfiguration : IEntityTypeConfiguration<LigneEntre>
{
    public void Configure(EntityTypeBuilder<LigneEntre> b)
    {
        b.ToTable("LigneEntres");
        b.HasKey(l => l.Id);
        b.Property(l => l.ArticleId).IsRequired();
        b.Property(l => l.Quantity).IsRequired().HasPrecision(18, 4);
        b.Property(l => l.Price).IsRequired().HasPrecision(18, 4);
    }
}

// ── BonSortie ─────────────────────────────────────────────────────────────────
internal sealed class BonSortieConfiguration : IEntityTypeConfiguration<BonSortie>
{
    public void Configure(EntityTypeBuilder<BonSortie> b)
    {
        b.ToTable("BonSorties");
        b.HasKey(x => x.Id);
        b.Property(x => x.Numero).IsRequired().HasMaxLength(50);
        b.Property(x => x.Observation).HasMaxLength(1000);
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt)
                 .IsConcurrencyToken(false)
                 .ValueGeneratedNever()
                 .UsePropertyAccessMode(PropertyAccessMode.Property);


        b.HasIndex(x => x.Numero)
         .IsUnique()
         .HasDatabaseName("IX_BonSorties_Numero")
         .HasFilter("[IsDeleted] = 0");

        b.HasMany(x => x.Lignes)
         .WithOne(l => l.BonSortie)
         .HasForeignKey(l => l.BonSortieId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

// ── LigneSortie ───────────────────────────────────────────────────────────────
internal sealed class LigneSortieConfiguration : IEntityTypeConfiguration<LigneSortie>
{
    public void Configure(EntityTypeBuilder<LigneSortie> b)
    {
        b.ToTable("LigneSorties");
        b.HasKey(l => l.Id);
        b.Property(l => l.ArticleId).IsRequired();
        b.Property(l => l.Quantity).IsRequired().HasPrecision(18, 4);
        b.Property(l => l.Price).IsRequired().HasPrecision(18, 4);
    }
}

// ── BonRetour ─────────────────────────────────────────────────────────────────
internal sealed class BonRetourConfiguration : IEntityTypeConfiguration<BonRetour>
{
    public void Configure(EntityTypeBuilder<BonRetour> b)
    {
        b.ToTable("BonRetours");
        b.HasKey(x => x.Id);
        b.Property(x => x.Numero).IsRequired().HasMaxLength(50);
        b.Property(x => x.Motif).IsRequired().HasMaxLength(500);
        b.Property(x => x.Observation).HasMaxLength(1000);
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.UpdatedAt)
                 .IsConcurrencyToken(false)
                 .ValueGeneratedNever()
                 .UsePropertyAccessMode(PropertyAccessMode.Property);


        b.Property(x => x.SourceType)
         .IsRequired()
         .HasConversion<string>()
         .HasMaxLength(20);

        b.HasIndex(x => x.Numero)
         .IsUnique()
         .HasDatabaseName("IX_BonRetours_Numero")
         .HasFilter("[IsDeleted] = 0");

        b.HasMany(x => x.Lignes)
         .WithOne(l => l.BonRetour)
         .HasForeignKey(l => l.BonRetourId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

// ── LigneRetour ───────────────────────────────────────────────────────────────
internal sealed class LigneRetourConfiguration : IEntityTypeConfiguration<LigneRetour>
{
    public void Configure(EntityTypeBuilder<LigneRetour> b)
    {
        b.ToTable("LigneRetours");
        b.HasKey(l => l.Id);
        b.Property(l => l.ArticleId).IsRequired();
        b.Property(l => l.Quantity).IsRequired().HasPrecision(18, 4);
        b.Property(l => l.Price).IsRequired().HasPrecision(18, 4);
        b.Property(l => l.Remarque).HasMaxLength(500);
    }
}