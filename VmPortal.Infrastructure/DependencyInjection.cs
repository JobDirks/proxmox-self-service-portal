using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VmPortal.Infrastructure.Data;

namespace VmPortal.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var conn = configuration.GetConnectionString("Default") ?? "Data Source=./data/vmportal.db";
            services.AddDbContext<VmPortalDbContext>(options => options.UseSqlite(conn));
            return services;
        }
    }
}
