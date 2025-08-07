using CommonLibraryP.API;
using CommonLibraryP.MachinePKG;
using CommonLibraryP.ShopfloorPKG;
using DevExpress.XtraPrinting.Shape.Native;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MapPKG
{
    public class MapService
    {

        private readonly IServiceScopeFactory scopeFactory;
        public MapService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        public async Task<List<MapConfig>> GetAllMapsConfig()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MapDBContext>();
                return await dbContext.MapConfigs.Include(x => x.MapComponents).AsNoTracking().ToListAsync();
            }
        }
        public async Task<MapConfig?> GetMapConfigById(Guid id)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MapDBContext>();
                return await dbContext.MapConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            }
        }
        public async Task<MapConfig?> GetMapAndComponentsById(string id)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MapDBContext>();
                return await dbContext.MapConfigs.Include(x=>x.MapComponents).AsNoTracking().FirstOrDefaultAsync(x => x.Id == new Guid(id));
            }
        }
        public async Task<RequestResult> UpsertMapConfig(MapConfig mapConfig)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MapDBContext>();
                    var target = dbContext.MapConfigs.FirstOrDefault(x => x.Id == mapConfig.Id);
                    if (target != null)
                    {
                        dbContext.Entry(target).CurrentValues.SetValues(mapConfig);
                    }
                    else
                    {
                        await dbContext.AddAsync(mapConfig);
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"Upsert map {mapConfig.Name} success");
                }
                catch (Exception e)
                {
                    return new(4, $"Upsert workorder {mapConfig.Name} fail({e.Message})");
                }

            }
        }
        public async Task<RequestResult> DeleteMapConfig(MapConfig mapConfig)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MapDBContext>();
                    var target = dbContext.MapConfigs.FirstOrDefault(x => x.Id == mapConfig.Id);
                    if (target != null)
                    {
                        dbContext.MapConfigs.Remove(target);
                        await dbContext.SaveChangesAsync();
                        return new(2, $"Delete map {mapConfig.Name} success");
                    }
                    else
                    {
                        return new(4, $"Map {mapConfig.Name} not found");
                    }
                }
                catch (Exception e)
                {
                    return new(4, $"Delete map {mapConfig.Name} fail({e.Message})");
                }
            }
        }

        public async Task<RequestResult> UpdateAllComponentsInMap(MapConfig mapConfig, IEnumerable<MapComponent> mapComponents)
        {
            var targetMap = await GetMapConfigById(mapConfig.Id);
            if (targetMap is not null)
            {
                if (mapComponents.Any(x => x.MapId != targetMap.Id))
                {
                    return new(4, "Components map id error");
                }
                var currentComponentsInMap = await GetComponentsInMapById(targetMap.Id);
                var componentsDelete = currentComponentsInMap.ExceptBy(mapComponents.Select(p => p.Id), p => p.Id).ToList();

                var deleteTasks = componentsDelete.Select(x => DeleteMapComponentTPC(x));
                var deleteTaskResults = await Task.WhenAll(deleteTasks);
                if (deleteTaskResults.Any(x => !x.IsSuccess))
                {
                    return new(4, "Delete not exist components fail");
                }

                var upsertTasks = mapComponents.Select(x => UpsertMapComponentTPC(x));
                var upsertTaskResults = await Task.WhenAll(upsertTasks);
                if (upsertTaskResults.Any(x => !x.IsSuccess))
                {
                    return new(4, "Upsert components fail");
                }

                return new(2, $"Update map {mapConfig.Name} all components success");

            }
            else
            {
                return new(4, "Map not found");
            }
        }

        private async Task<List<MapComponent>> GetComponentsInMapById(Guid mapId)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MapDBContext>();
                return await dbContext.MapComponents.AsNoTracking().Where(x => x.MapId == mapId).ToListAsync();
            }
        }

        public async Task<RequestResult> UpsertMapComponentTPC(MapComponent mapComponent)
        {
            if (mapComponent is MapComponentStation mapComponentStation)
            {
                return await UpsertMapComponent<MapComponentStation>(mapComponentStation);
            }
            else if (mapComponent is MapComponentMachine mapComponentMachine)
            {
                return await UpsertMapComponent<MapComponentMachine>(mapComponentMachine);
            }
            else
            {
                return new(4, $"Upsert mapComponent fail(downcasting fail)");
            }
        }

        private async Task<RequestResult> UpsertMapComponent<T>(T component) where T : MapComponent
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MapDBContext>();
                    var target = await dbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == component.Id);
                    if (target is not null)
                    {
                        dbContext.Entry<T>(target).CurrentValues.SetValues(component);
                    }
                    else
                    {
                        dbContext.Set<T>().Add(component);
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"Upsert component success");
                }
                catch (Exception e)
                {
                    return new(4, $"Upsert component fail({e.Message})");
                }

            }
        }

        public async Task<List<Machine>> GetMachinesFromMchineService()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var machineService = scope.ServiceProvider.GetService<MachineService>();
                return machineService is null ? new List<Machine>() : await machineService.GetAllMachinesConfig();
            }
        }

        public async Task<List<Station>> GetStationsFromShopfloorService()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var shopfloorService = scope.ServiceProvider.GetService<ShopfloorService>();
                return shopfloorService is null ? new List<Station>() : await shopfloorService.GetAllStationConfigs();
            }
        }

        public async Task<RequestResult> DeleteMapComponentTPC(MapComponent mapComponent)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                if (mapComponent is MapComponentStation mapComponentStation)
                {
                    return await DeleteMapComponent<MapComponentStation>(mapComponentStation);
                }
                else if (mapComponent is MapComponentMachine mapComponentMachine)
                {
                    return await DeleteMapComponent<MapComponentMachine>(mapComponentMachine);
                }
                else
                {
                    return new(4, $"Delete component fail(downcasting fail)");
                }
            }
        }

        private async Task<RequestResult> DeleteMapComponent<T>(T mapComponent) where T : MapComponent
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MapDBContext>();
                    var target = await dbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == mapComponent.Id);
                    if (target is not null)
                    {
                        dbContext.Entry(target).State = EntityState.Deleted;
                    }
                    else
                    {
                        return new(4, $"Component not found");
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"Delete component success");
                }
                catch (Exception e)
                {
                    return new(4, $"Delete component fail({e.Message})");
                }
            }
        }
    }
}
