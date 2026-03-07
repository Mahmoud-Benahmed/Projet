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
                entity.Property(a => a.CodeRef)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(a => a.Libelle)
                      .IsRequired()
                      .HasMaxLength(250);
                
                entity.Property(a => a.Prix)
                      .HasColumnType("decimal(18,2)");
                
                entity.Property(a => a.IsActive)
                      .HasDefaultValue(true);
                
                entity.Property(a => a.TVA)
                      .HasPrecision(5, 2);
                
                entity.Property(a => a.BarCode)
                      .HasMaxLength(13);
                
                entity.Property(a => a.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
                
                entity.Property(a => a.UpdatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
                

                
                // Relationship to Category
                entity.HasOne(a => a.Category)
                      .WithMany()
                      .HasForeignKey(a => a.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ArticleCode
            modelBuilder.Entity<ArticleCode>(entity =>
            {
                entity.ToTable("ArticleCodes");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Prefix)
                      .HasMaxLength(10)
                      .IsRequired();
                entity.Property(c => c.LastNumber)
                      .HasDefaultValue(0);
                entity.Property(c => c.Padding)
                      .HasDefaultValue(6);
            });

            // Configure Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");

                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(c => c.TVA)
                      .HasPrecision(5, 2);
            });
        }
    }
}