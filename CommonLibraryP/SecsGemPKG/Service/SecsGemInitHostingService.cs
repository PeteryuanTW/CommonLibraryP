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
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var secsGemService = scope.ServiceProvider.GetRequiredService<SecsGemService>();
                await secsGemService.InitAndStartHSMS();
                secsGemService.InitGem();
                await secsGemService.InitSVs();

                while (!stoppingToken.IsCancellationRequested)
                {
                    if (secsGemService.GemStatus.UpdateSV)
                    {
                        await secsGemService.UpdateSVs();
                        await secsGemService.UpdateGemStatus();
                    }
                    await Task.Delay(secsGemService.GemStatus.UpdateSVDelay, stoppingToken);
                }
            }
        }
    }
}
