using BitzArt.Blazor.Cookies;
using CommonLibraryP.NotificationUtility;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.UserPKG.Extension
{
    public static class UserExtension
    {
        public static IHostApplicationBuilder AddUserService(this IHostApplicationBuilder builder, string dbConnectionStringName = "DefaultConnection", double timeoutHour = 0.5)
        {
            builder.Services.AddScoped<UserService>();
            builder.Services.TryAddScoped<NotificationService>();
            builder.Services.AddDbContextFactory<UserDBContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString(dbConnectionStringName));
            });
            builder.AddBlazorCookies();
            builder.Services.AddDataProtection();

            return builder;

        }
    }
}
