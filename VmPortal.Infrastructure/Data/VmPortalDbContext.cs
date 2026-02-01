using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VmPortal.Domain.Users;
using VmPortal.Domain.Vms;
using VmPortal.Domain.Auth;
using VmPortal.Domain.Actions;
using VmPortal.Domain.Security;

namespace VmPortal.Infrastructure.Data
{
    public class VmPortalDbContext(DbContextOptions<VmPortalDbContext> options)
        : DbContext(options), IDataProtectionKeyContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Vm> Vms => Set<Vm>();
        public DbSet<AuthEvent> AuthEvents => Set<AuthEvent>();
        public DbSet<UserAction> UserActions => Set<UserAction>();
        public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
        public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

        public DbSet<VmTemplate> VmTemplates => Set<VmTemplate>();
        public DbSet<VmUserPermission> VmUserPermissions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new Configurations.UserConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.VmConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.AuthEventConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.UserActionConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.SecurityEventConfiguration());
            modelBuilder.ApplyConfiguration(new Configurations.VmTemplateConfiguration());
        }
    }
}
