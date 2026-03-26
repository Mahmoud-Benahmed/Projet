using Microsoft.EntityFrameworkCore;
using ERP.ArticleService.Domain;

namespace ERP.ArticleService.Infrastructure.Persistence
{
    public class ArticleDbContext : DbContext
    {
        public ArticleDbContext(DbContextOptions<ArticleDbContext> options)
            : base(options)
        {
        }

        public DbSet<Article> Articles { get; set; }
        public DbSet<ArticleCode> ArticleCodes { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Article
            modelBuilder.Entity<Article>(entity =>
            {
                entity.ToTable("Articles");

                entity.HasKey(a => a.Id);

                entity.Property(a => a.Id)
                      .ValueGeneratedNever();

                entity.Property(a => a.CodeRef)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(a => a.Libelle)
                      .IsRequired()
                      .HasMaxLength(250);

                entity.Property(a => a.Prix)
                      .HasColumnType("decimal(18,2)");

                entity.Property(a => a.IsDeleted)
                      .HasDefaultValue(false);

                entity.Property(a => a.TVA)
                      .HasPrecision(5, 2);

                entity.Property(a => a.BarCode)
                      .HasMaxLength(13);

                entity.Property(a => a.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(a => a.UpdatedAt)
                      .IsRequired(false);

                // Relationship to Category
                entity.HasOne(a => a.Category)
                      .WithMany()
                      .HasForeignKey(a => a.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Unique indexes (active articles only)
                entity.HasIndex(a => a.CodeRef)
                      .IsUnique()
                      .HasFilter("[IsDeleted] = 0");

                entity.HasIndex(a => a.BarCode)
                      .IsUnique()
                      .HasFilter("[IsDeleted] = 0 AND [BarCode] IS NOT NULL");

                // Global soft-delete filter
                entity.HasQueryFilter(a => !a.IsDeleted);
            });

            // Configure ArticleCode
            modelBuilder.Entity<ArticleCode>(entity =>
            {
                entity.ToTable("ArticleCodes");

                entity.HasKey(c => c.Id);

                entity.Property(c => c.Id)
                      .ValueGeneratedNever();

                entity.Property(c => c.Prefix)
                      .HasMaxLength(10)
                      .IsRequired();

                entity.Property(c => c.LastNumber)
                      .HasDefaultValue(0);

                entity.Property(c => c.Padding)
                      .HasDefaultValue(6);

                // Unique index on Prefix
                entity.HasIndex(c => c.Prefix)
                      .IsUnique();
            });

            // Configure Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");

                entity.HasKey(c => c.Id);

                entity.Property(c => c.Id)
                      .ValueGeneratedNever();

                entity.Property(c => c.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(c => c.TVA)
                      .HasPrecision(5, 2);

                // Unique index on Name
                entity.HasIndex(c => c.Name)
                      .IsUnique();


                entity.Property(c => c.IsDeleted)
                      .HasDefaultValue(false);
                entity.Property(c => c.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(c => c.UpdatedAt)
                      .IsRequired(false);

                entity.HasQueryFilter(c => !c.IsDeleted);
            });
        }
    }
}