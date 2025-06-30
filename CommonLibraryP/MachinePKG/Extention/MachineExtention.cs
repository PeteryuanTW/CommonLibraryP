using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using CommonLibraryP.MachinePKG.Service;
using CommonLibraryP.NotificationUtility;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommonLibraryP.MachinePKG
{
    public static class MachineExtention
    {
        public static IHostApplicationBuilder AddMachineService(this IHostApplicationBuilder builder, string dbConnectionStringName = "DefaultConnection")
        {
            builder.Services.AddDbContextFactory<MachineDBContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString(dbConnectionStringName));
            });
            builder.Services.AddSingleton<MachineService>();
            builder.Services.AddHostedService<MachineInitHostingService>();
            builder.Services.TryAddScoped<NotificationService>();
            //builder.Services.AddHostedService<ConditionHostService>();
            //builder.Services.TryAddScoped<NotificationService>();
            builder.Services.AddLocalization();
            return builder;
        }

        public static IHostApplicationBuilder AddMachineService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] MachineServiceImplementation>(this IHostApplicationBuilder builder, string dbConnectionStringName = "DefaultConnection")
        where MachineServiceImplementation : MachineService
        {
            builder.Services.AddDbContextFactory<MachineDBContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString(dbConnectionStringName));
            });
            builder.Services.AddSingleton<MachineService, MachineServiceImplementation>();
            builder.Services.AddHostedService<MachineInitHostingService>();
            //builder.Services.AddHostedService<ConditionHostService>();
            builder.Services.AddLocalization();
            return builder;
        }
    }
}
