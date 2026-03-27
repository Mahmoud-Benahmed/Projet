using ERP.ClientService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.ClientService.Infrastructure.Persistence;

public sealed class ClientDbContext(DbContextOptions<ClientDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ClientCategory> ClientCategories => Set<ClientCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClientDbContext).Assembly);
}

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> b)
    {
        b.ToTable("Clients");
        b.HasKey(c => c.Id);

        b.Property(c => c.Name).IsRequired().HasMaxLength(200);
        b.Property(c => c.Email).IsRequired().HasMaxLength(200);
        b.Property(c => c.Address).IsRequired().HasMaxLength(500);
        b.Property(c => c.Phone).HasMaxLength(20);
        b.Property(c => c.TaxNumber).HasMaxLength(50);
        b.Property(c => c.CreditLimit).HasPrecision(18, 4);
        b.Property(c => c.IsBlocked).IsRequired();
        b.Property(c => c.IsDeleted).IsRequired();
        b.Property(c => c.CreatedAt).IsRequired();

        // Filtered unique index — email must be unique only among non-deleted clients.
        // A deleted client's email can be reused by a new registration.
        b.HasIndex(c => c.Email)
         .IsUnique()
         .HasFilter("[IsDeleted] = 0");

        b.HasIndex(c => c.IsBlocked);

        // Global soft-delete filter — all queries exclude deleted clients automatically.
        // Use .IgnoreQueryFilters() in repository methods that need deleted records.
        b.HasQueryFilter(c => !c.IsDeleted);
    }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.HasKey(c => c.Id);

        b.Property(c => c.Name).IsRequired().HasMaxLength(200);
        b.Property(c => c.Code).IsRequired().HasMaxLength(50);
        b.Property(c => c.DelaiRetour).IsRequired();
        b.Property(c => c.DiscountRate).HasPrecision(5, 4);
        b.Property(c => c.CreditLimitMultiplier).HasPrecision(8, 4);
        b.Property(c => c.IsActive).IsRequired();
        b.Property(c => c.IsDeleted).IsRequired();

        // Filtered unique index — code must be unique only among non-deleted categories.
        b.HasIndex(c => c.Code)
         .IsUnique()
         .HasFilter("[IsDeleted] = 0");

        b.HasIndex(c => c.IsActive);

        // Global soft-delete filter
        b.HasQueryFilter(c => !c.IsDeleted);
    }
}

internal sealed class ClientCategoryConfiguration : IEntityTypeConfiguration<ClientCategory>
{
    public void Configure(EntityTypeBuilder<ClientCategory> b)
    {
        b.ToTable("ClientCategories");

        b.HasKey(cc => new { cc.ClientId, cc.CategoryId });

        b.Property(cc => cc.AssignedById).IsRequired();
        b.Property(cc => cc.AssignedAt).IsRequired();

        b.HasOne(cc => cc.Client)
         .WithMany(c => c.ClientCategories)
         .HasForeignKey(cc => cc.ClientId)
         .IsRequired()
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(cc => cc.Category)
         .WithMany(c => c.ClientCategories)
         .HasForeignKey(cc => cc.CategoryId)
         .IsRequired()
         .OnDelete(DeleteBehavior.Restrict);
    }
}