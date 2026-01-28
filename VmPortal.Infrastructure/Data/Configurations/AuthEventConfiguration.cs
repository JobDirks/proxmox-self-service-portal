using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmPortal.Domain.Auth;

namespace VmPortal.Infrastructure.Data.Configurations
{
    public class AuthEventConfiguration : IEntityTypeConfiguration<AuthEvent>
    {
        public void Configure(EntityTypeBuilder<AuthEvent> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.EventType).IsRequired().HasMaxLength(40);
            b.Property(x => x.SourceIP).HasMaxLength(64);
            b.Property(x => x.UserAgent).HasMaxLength(512);
            b.Property(x => x.OccurredAt).IsRequired();

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.OccurredAt);
        }
    }
}
