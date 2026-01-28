using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VmPortal.Domain.Vms;

namespace VmPortal.Infrastructure.Data.Configurations
{
    public class VmTemplateConfiguration : IEntityTypeConfiguration<VmTemplate>
    {
        public void Configure(EntityTypeBuilder<VmTemplate> b)
        {
            b.HasKey(x => x.Id);

            b.Property(x => x.Name)
             .IsRequired()
             .HasMaxLength(100);

            b.Property(x => x.Node)
             .IsRequired()
             .HasMaxLength(100);

            b.Property(x => x.TemplateVmId)
             .IsRequired();

            b.Property(x => x.DefaultCpuCores)
             .IsRequired();

            b.Property(x => x.DefaultMemoryMiB)
             .IsRequired();

            b.Property(x => x.DefaultDiskGiB)
             .IsRequired();

            b.Property(x => x.IsActive)
             .IsRequired();

            b.Property(x => x.Description)
             .HasMaxLength(500);

            b.Property(x => x.CreatedAt)
             .IsRequired();

            b.HasIndex(x => new { x.Node, x.TemplateVmId })
             .IsUnique();

            b.HasIndex(x => x.IsActive);
        }
    }
}
