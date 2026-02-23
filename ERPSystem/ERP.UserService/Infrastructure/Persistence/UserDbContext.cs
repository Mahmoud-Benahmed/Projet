using ERP.UserService.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.UserService.Infrastructure.Persistence;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options)
        : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.AuthUserId).IsUnique();

            entity.Property(x => x.Email)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.HasIndex(x => x.Email)
                  .IsUnique();

            entity.Property(x => x.FullName)
                  .HasMaxLength(150);

            entity.Property(x => x.Phone)
                  .HasMaxLength(20);
        });
    }
}