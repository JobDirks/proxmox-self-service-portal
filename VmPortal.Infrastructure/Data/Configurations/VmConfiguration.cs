using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmPortal.Domain.Vms;

namespace VmPortal.Infrastructure.Data.Configurations
{
    public class VmConfiguration : IEntityTypeConfiguration<Vm>
    {
        public void Configure(EntityTypeBuilder<Vm> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.VmId).IsRequired();
            b.Property(x => x.Node).IsRequired().HasMaxLength(100);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.CpuCores).IsRequired();
            b.Property(x => x.MemoryMiB).IsRequired();
            b.Property(x => x.DiskGiB).IsRequired();
            b.Property(x => x.LastSyncedAt).IsRequired();

            b.HasIndex(x => new { x.Node, x.VmId }).IsUnique();
            b.HasIndex(x => x.OwnerUserId);

            b.HasOne(x => x.Owner)
             .WithMany(u => u.Vms)
             .HasForeignKey(x => x.OwnerUserId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
