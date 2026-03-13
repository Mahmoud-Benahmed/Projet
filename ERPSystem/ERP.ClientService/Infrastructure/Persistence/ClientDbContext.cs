using ERP.ClientService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.ClientService.Infrastructure.Persistence
{
    public class ClientDbContext : DbContext
    {
        public ClientDbContext(DbContextOptions<ClientDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clients");

                entity.HasKey(c => c.Id);
                entity.Property(c => c.Id)
                      .ValueGeneratedNever();

                entity.Property(c => c.Type)
                      .IsRequired()
                      .HasConversion<string>()
                      .HasMaxLength(50);

                entity.Property(c => c.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(c => c.Email)
                      .IsRequired()
                      .HasMaxLength(250);

                entity.Property(c => c.Address)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(c => c.Phone)
                      .HasMaxLength(50)
                      .IsRequired(false);

                entity.Property(c => c.TaxNumber)
                      .HasMaxLength(100)
                      .IsRequired(false);

                entity.Property(c => c.IsDeleted)
                      .HasDefaultValue(false);

                entity.Property(c => c.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(c => c.UpdatedAt)
                      .IsRequired(false);

                entity.HasIndex(c => c.Email)
                      .IsUnique()
                      .HasFilter("[IsDeleted] = 0");

                entity.HasIndex(c => c.Name)
                       .IsUnique()
                       .HasFilter("[IsDeleted] = 0");

                entity.HasQueryFilter(c => !c.IsDeleted);
            });
        }
    }
}