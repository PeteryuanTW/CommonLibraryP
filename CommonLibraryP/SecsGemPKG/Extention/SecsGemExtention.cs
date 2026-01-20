using CommonLibraryP.MachinePKG;
using CommonLibraryP.MapPKG;
using CommonLibraryP.MapPKG.Component;
using CommonLibraryP.NotificationUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public static class SecsGemExtention
    {
        public static IHostApplicationBuilder AddSecsGemService(this IHostApplicationBuilder builder, string dbConnectionStringName = "DefaultConnection")
        {
            builder.Services.AddOptions<HSMSParameter>()
                .Bind(builder.Configuration.GetSection("HSMSParameter"))
                .ValidateDataAnnotations();
            builder.Services.AddDbContextFactory<SecsGemDBContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString(dbConnectionStringName));
            });
            builder.Services.AddSingleton<SecsGemService>();
            builder.Services.AddHostedService<SecsGemInitHostingService>();
            return builder;
        }
    }
}
