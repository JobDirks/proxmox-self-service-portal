using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmPortal.Domain.Security;

namespace VmPortal.Infrastructure.Data.Configurations
{
    public class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
    {
        public void Configure(EntityTypeBuilder<SecurityEvent> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).IsRequired().HasMaxLength(50);
            b.Property(x => x.UserId).IsRequired().HasMaxLength(200);
            b.Property(x => x.IpAddress).HasMaxLength(45);
            b.Property(x => x.UserAgent).HasMaxLength(512);
            b.Property(x => x.Details).HasMaxLength(4000);
            b.Property(x => x.Severity).IsRequired().HasMaxLength(20);
            b.Property(x => x.OccurredAt).IsRequired();

            b.HasIndex(x => x.EventType);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.OccurredAt);
            b.HasIndex(x => x.Severity);
        }
    }
}
