using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG.Service
{
    public class ConditionHostService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        public ConditionHostService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var machineService = scope.ServiceProvider.GetRequiredService<MachineService>();
                //await machineService.InitConditions();
            }
        }
    }
}
