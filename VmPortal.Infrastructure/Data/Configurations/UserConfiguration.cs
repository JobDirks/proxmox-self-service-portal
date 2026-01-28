using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmPortal.Domain.Users;

namespace VmPortal.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.ExternalId).IsRequired().HasMaxLength(200);
            b.Property(x => x.Username).IsRequired().HasMaxLength(100);
            b.Property(x => x.Email).IsRequired().HasMaxLength(320);
            b.Property(x => x.DisplayName).HasMaxLength(200);
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.IsActive).IsRequired();

            b.HasIndex(x => x.ExternalId).IsUnique();
            b.HasIndex(x => x.Username).IsUnique();
        }
    }
}
