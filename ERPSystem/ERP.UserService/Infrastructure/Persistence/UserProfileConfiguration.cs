using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ERP.UserService.Domain;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.AuthUserId).IsUnique();

        builder.Property(x => x.Email)
               .IsRequired()
               .HasMaxLength(200);
    }
}