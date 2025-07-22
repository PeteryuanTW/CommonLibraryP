using CommonLibraryP.MachinePKG;
using CommonLibraryP.NotificationUtility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.LogPKG
{
    public static class LogExtension
    {
        public static IHostApplicationBuilder AddLogService(this IHostApplicationBuilder builder, string dbConnectionStringName = "DefaultConnection")
        {
            var connectionStringFromConfig = builder.Configuration.GetConnectionString(dbConnectionStringName);
            var connectionString = string.IsNullOrEmpty(connectionStringFromConfig) ? dbConnectionStringName : connectionStringFromConfig;

            builder.Services.AddSingleton<SerilogService>(provider =>
            {
                return new SerilogService(connectionString);
            });
            builder.Services.TryAddSingleton<NotificationService>();
            return builder;
        }
    }
}
