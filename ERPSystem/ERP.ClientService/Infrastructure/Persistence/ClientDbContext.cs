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
        b.Property(c => c.DelaiRetour);        // nullable int — no IsRequired
        b.Property(c => c.DuePaymentPeriod);   // nullable int — no IsRequired
        b.Property(c => c.IsBlocked).IsRequired();
        b.Property(c => c.IsDeleted).IsRequired();
        b.Property(c => c.CreatedAt).IsRequired();

        b.HasIndex(c => c.Email)
         .IsUnique()
         .HasFilter("[IsDeleted] = 0");

        b.HasIndex(c => c.IsBlocked);

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
        b.Property(c => c.DuePaymentPeriod).IsRequired();  // ← was missing IsRequired
        b.Property(c => c.DiscountRate).HasPrecision(5, 4);
        b.Property(c => c.CreditLimitMultiplier).HasPrecision(8, 4);
        b.Property(c => c.UseBulkPricing).IsRequired();
        b.Property(c => c.IsActive).IsRequired();
        b.Property(c => c.IsDeleted).IsRequired();
        b.Property(c => c.CreatedAt).IsRequired();

        b.HasIndex(c => c.Code)
         .IsUnique()
         .HasFilter("[IsDeleted] = 0");

        b.HasIndex(c => c.IsActive);

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

        b.HasQueryFilter(cc => !cc.Category!.IsDeleted);
    }
}