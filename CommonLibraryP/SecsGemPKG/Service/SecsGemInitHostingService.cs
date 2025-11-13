using CommonLibraryP.MachinePKG;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SecsGemInitHostingService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        public SecsGemInitHostingService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var secsGemService = scope.ServiceProvider.GetRequiredService<SecsGemService>();
                secsGemService.InitHSMSFromSetting();
                return Task.CompletedTask;
            }
        }
    }
}
