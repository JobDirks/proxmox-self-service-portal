using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmPortal.Domain.Actions;

namespace VmPortal.Infrastructure.Data.Configurations
{
    public class UserActionConfiguration : IEntityTypeConfiguration<UserAction>
    {
        public void Configure(EntityTypeBuilder<UserAction> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.ActionName).IsRequired().HasMaxLength(80);
            b.Property(x => x.TargetType).IsRequired().HasMaxLength(50);
            b.Property(x => x.TargetId).IsRequired().HasMaxLength(200);
            b.Property(x => x.MetadataJson).HasMaxLength(4000);
            b.Property(x => x.OccurredAt).IsRequired();

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.OccurredAt);
        }
    }
}
