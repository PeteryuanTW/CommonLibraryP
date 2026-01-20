using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CommonLibraryP.NotificationUtility;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommonLibraryP.MapPKG
{
    public static class MapExtension
    {
        public static IHostApplicationBuilder AddMapService(this IHostApplicationBuilder builder, string dbConnectionStringName = "DefaultConnection")
        {
            builder.Services.AddDbContextFactory<MapDBContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString(dbConnectionStringName));
            });
            builder.Services.AddSingleton<MapService>();
            builder.Services.TryAddScoped<NotificationService>();
            builder.Services.AddLocalization();
            return builder;
        }
    }
}
