using CommonLibraryP.API;
using CommonLibraryP.Data;
using CommonLibraryP.MachinePKG;
using DevExpress.Data.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace CommonLibraryP.ShopfloorPKG
{
    public class ShopfloorService
    {
        private readonly IServiceScopeFactory scopeFactory;
        public ShopfloorService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        #region process
        public async Task<List<Process>> GetAllProcesses()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                return await dbContext.Processes.AsNoTracking().ToListAsync();
            }
        }
        public async Task<RequestResult> UpsertProcess(Process process)
        {
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                    Process? targetProcess = dbContext.Processes.Include(x => x.Stations).FirstOrDefault(x => x.Id == process.Id);
                    if (targetProcess != null)
                    {
                        targetProcess.Name = process.Name;
                    }
                    else
                    {
                        await dbContext.Processes.AddAsync(process);
                    }
                    await dbContext.SaveChangesAsync();
                    return new RequestResult(2, $"Upsert process {process.Name} success");
                }
            }
            catch (Exception ex)
            {
                return new RequestResult(4, $"Upsert process {process.Name} fail({ex.Message})");
            }
        }
        public async Task<RequestResult> DeleteProcess(Process process)
        {
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                    Process? targetProcess = dbContext.Processes.Include(x => x.Stations).FirstOrDefault(x => x.Id == process.Id);
                    if (targetProcess != null)
                    {
                        dbContext.Remove(targetProcess);
                        await dbContext.SaveChangesAsync();
                        return new RequestResult(2, $"Delete process {targetProcess.Name} success");
                    }
                    else
                    {
                        return new RequestResult(4, $"Process {process.Name} not found");
                    }

                }
            }
            catch (Exception ex)
            {
                return new RequestResult(4, $"Delete process {process.Name} fail({ex.Message})");
            }
        }
        public async Task<List<Process>> GetAllProcessAndStations()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                return await dbContext.Processes.Include(x => x.Stations.OrderBy(x => x.ProcessIndex).ThenBy(x => x.Name)).AsNoTracking().ToListAsync();
            }
        }
        public Task<List<ProcessMachineRelation>> GetProcessMachineRelationByID(Guid? id)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                var targetMachinesId = dbContext.ProcessMachineRelations.Where(x => x.ProcessId == id);
                return Task.FromResult(targetMachinesId.ToList());
            }
        }
        public async Task<Process?> GetProcessByName(string processName)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                return await dbContext.Processes.FirstOrDefaultAsync(x => x.Name == processName);
            }
        }
        public async Task<Process?> GetProcessByID(Guid? processID)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                return await dbContext.Processes.FirstOrDefaultAsync(x => x.Id == processID);
            }
        }

        public async Task<List<Station>> GetStationsByProcessID(Guid processID)
        {
            Process? targetProcess = await GetProcessByID(processID);
            if (targetProcess is not null)
            {
                return Stations.Where(x => x.ProcessId == targetProcess.Id).ToList();
            }
            return new();
        }

        private async Task<bool> CheckStationIsLastInProcess(Station station)
        {
            var stations = await GetStationsByProcessID(station.ProcessId);
            return stations.Where(x => x.Enable == true).Max(x => x.ProcessIndex) == station.ProcessIndex;
        }
        #endregion

        #region station

        private List<Station> stations = new List<Station>();
        public List<Station> Stations => stations;

        public Func<Task>? StationStatuschangedAct;

        protected void StationStatechanged()
        {
            StationStatuschangedAct?.Invoke();
        }

        public async Task<List<Station>> GetAllStationConfigs()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                return await dbContext.Stations.AsNoTracking().ToListAsync();
            }
        }
        public virtual List<StationTypeWrapperClass> GetStationTypesWrapperClass()
        {
            return ShopfloorTypeEnumHelper.GetStationTypesWrapperClass().ToList();
        }
        public async Task<IEnumerable<Machine>> GetMachineConfigs()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var machineService = scope.ServiceProvider.GetRequiredService<MachineService>();
                    return await machineService.GetAllMachinesConfig();
                }
                catch
                {
                    return new List<Machine>();
                }

            }
        }
        public async Task<RequestResult> UpsertStation(Station station)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                    Station? target = dbContext.Stations.FirstOrDefault(x => x.Id == station.Id);
                    if (target != null)
                    {
                        target.ProcessId = station.ProcessId;
                        target.Name = station.Name;
                        target.ProcessIndex = station.ProcessIndex;
                        target.StationType = station.StationType;
                        target.Enable = station.Enable;

                    }
                    else
                    {
                        await dbContext.Stations.AddAsync(station);
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"Upsert station {station.Name} success");
                }
                catch (Exception ex)
                {
                    return new RequestResult(4, $"Upsert station {station.Name} fail({ex.Message})");
                }

            }
        }
        public async Task<RequestResult> DeleteStation(Station station)
        {
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                    Station? targetStation = dbContext.Stations.FirstOrDefault(x => x.Id == station.Id);
                    if (targetStation != null)
                    {
                        dbContext.Remove(targetStation);
                        await dbContext.SaveChangesAsync();
                        return new RequestResult(2, $"Delete station {targetStation.Name} success");
                    }
                    else
                    {
                        return new RequestResult(4, $"Process {station.Name} not found");
                    }

                }
            }
            catch (Exception ex)
            {
                return new RequestResult(4, $"Delete station {station.Name} fail({ex.Message})");
            }
        }
        protected virtual Station InitMachineToDerivesClass(Station station)
        {
            Station res;
            switch (station.StationType)
            {
                case 11:
                    res = new StationSingleWorkorderSingleSerial(station);
                    break;
                case 12:
                    res = new StationSingleWorkorderMultipleSerials(station);
                    break;
                default:
                    res = station;
                    break;
            }
            //res.MachineStatechangedRecordAct += MachineStatusChangedRecord;
            return res;
        }
        public async Task InitAllStation()
        {
            stations = new();
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                var stationbases = await dbContext.Stations.Include(x => x.Process).AsNoTracking().ToListAsync();
                foreach (var stationbase in stationbases)
                {
                    stations.Add(InitMachineToDerivesClass(stationbase));
                }
                stations.ForEach(x => x.InitStation());
                //var retrieveTasks = stations.Select(x => RetrieveWorkorderItemsAndTaskInStation(x));
                foreach (var s in stations)
                {
                    await RetrieveWorkorderItemsAndTaskInStation(s);
                }
                //await Task.WhenAll(retrieveTasks);
            }
        }
        private async Task RetrieveWorkorderItemsAndTaskInStation(Station station)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                var tasksInStation = await dbContext.TaskDetails.Where(x => x.StationId == station.Id && x.FinishedTime == null).AsNoTracking().ToListAsync();
                bool hasTask = tasksInStation.Count > 0;
                if (!hasTask)
                {
                    return;
                }
                bool singleTask = tasksInStation.Count == 1;

                var distinctItemIds = tasksInStation.Select(x => x.ItemId).Distinct().ToList();
                var ItemDetailsFromTasks = await dbContext.ItemDetails.Include(x=>x.ItemRecords).Where(x => distinctItemIds.Contains(x.Id) && x.FinishedTime == null).AsNoTracking().ToListAsync();
                bool hasItem = ItemDetailsFromTasks.Count > 0;
                if (!hasItem)
                {
                    return;
                }
                bool singleItem = ItemDetailsFromTasks.Count == 1;

                var distinctWorkorderIds = ItemDetailsFromTasks.Select(x => x.WorkordersId).Distinct().ToList();
                var workordersFrom = await dbContext.Workorders.Where(x => distinctWorkorderIds.Contains(x.Id) && x.FinishedTime == null).AsNoTracking().ToListAsync();
                bool hasWorkorder = workordersFrom.Count > 0;
                if (!hasItem)
                {
                    return;
                }
                bool singleWorkorder = workordersFrom.Count == 1;

                if (station.IsSingleWorkorder != singleWorkorder
                    || station.IsSingleItem != singleItem
                    || station.IsSingleItem != singleTask
                    || singleItem != singleTask)
                {
                    return;
                }

                switch (station.StationType)
                {
                    case 111:
                        station.SetWorkorder(workordersFrom.FirstOrDefault());
                        station.Run();
                        station.AddItemDetail(ItemDetailsFromTasks.FirstOrDefault());
                        station.AddTaskDetail(tasksInStation.FirstOrDefault());
                        break;
                    default:
                        break;
                }
            }
        }



        private Station? GetStationByName(string stationName)
        {
            return stations.FirstOrDefault(x => x.Name == stationName);
        }

        public async Task<RequestResult> DeployWorkorderToStation(Guid stationId, WorkorderIdModel WorkorderIdModel)
        {
            var workorder = await GetWorkorderById(WorkorderIdModel.WorkorderID);
            var station = stations.FirstOrDefault(x => x.Id == stationId);
            if (workorder is not null && station is not null)
            {
                return station.SetWorkorder(workorder);
                //return new(2, $"Deploy workorder {workorder.WorkorderNo}-{workorder.Lot} to station {station.Name} success");
            }
            return new(4, $"Workorder or station not found");
        }

        public Task<RequestResult> RunStation(Guid id)
        {
            var target = stations.FirstOrDefault(x => x.Id == id);
            if (target is not null)
            {
                var res = target.Run();
                StationStatechanged();
                return Task.FromResult<RequestResult>(res);
            }
            return Task.FromResult<RequestResult>(new(3, "Station not found"));
        }

        public async Task<RequestResult> StationInByNameAndSerialNo(SingleSarialNoStationInModel singleSarialNoStationInModel)
        {
            Station? targetStation = GetStationByName(singleSarialNoStationInModel.StationName);
            if (targetStation is null)
            {
                return new(3, $"station {singleSarialNoStationInModel.StationName} not found");
            }
            var checkStationInRes = targetStation.CheckCanAddItem();
            if (!checkStationInRes.IsSuccess)
            {
                return checkStationInRes;
            }
            switch (targetStation.StationType)
            {
                case 11:
                case 12:
                    try
                    {
                        StationSingleWorkorder? stationSingleWorkorder = targetStation as StationSingleWorkorder;
                        var itemDetail = await GetOrGenerateItem(stationSingleWorkorder.Workorder.Id, singleSarialNoStationInModel.SerialNo);
                        var addItemRes = stationSingleWorkorder.AddItemDetail(itemDetail);

                        if (!addItemRes.IsSuccess)
                        {
                            return addItemRes;
                        }

                        var taskDetail = await GetOrGenerateTask(itemDetail.Id, stationSingleWorkorder.Id);
                        var addTaskRes = stationSingleWorkorder.AddTaskDetail(taskDetail);
                        if (!addTaskRes.IsSuccess)
                        {
                            return addTaskRes;
                        }

                        return new RequestResult(2, $"Add item and task success");
                    }
                    catch (Exception ex)
                    {
                        return new(4, ex.Message);
                    }
                default:
                    return new(3, $"station {singleSarialNoStationInModel.StationName} deosn't support this command");
            }
        }

        public async Task<RequestResult> StationOutByFIFO(StationOutByFIFOModel fIFOStationOutModel)
        {
            Station? targetStation = GetStationByName(fIFOStationOutModel.StationName);
            if (targetStation is null)
            {
                return new(3, $"station {fIFOStationOutModel.StationName} not found");
            }
            var check = targetStation.CheckCanRemoveItem();
            if (!check.IsSuccess)
            {
                return check;
            }
            bool isLast = await CheckStationIsLastInProcess(targetStation);
            switch (targetStation.StationType)
            {
                case 11:
                    try
                    {
                        if (targetStation is StationSingleWorkorderSingleSerial stationSingleWorkorderSingleSerial)
                        {

                            var item = stationSingleWorkorderSingleSerial?.WIPItemDetail;
                            stationSingleWorkorderSingleSerial?.RemoveItemDetail();

                            var taskDetail = stationSingleWorkorderSingleSerial.WIPTaskDetail;
                            taskDetail.FinishedTime = DateTime.Now;
                            await UpsertTaskDetail(taskDetail);
                            stationSingleWorkorderSingleSerial?.RemoveTaskDetail();

                            if (isLast)
                            {
                                item.FinishedTime = DateTime.Now;
                                if (fIFOStationOutModel.Pass)
                                {
                                    item.Okamount++;
                                }
                                else
                                {
                                    item.Ngamount++;
                                }
                                return await UpsertItemDetail(item);
                            }
                            return new(2, $"Station {fIFOStationOutModel.StationName} station out by FIFO success");
                        }
                        else
                        {
                            return new(4, $"Station {fIFOStationOutModel.StationName} type downcasting error");
                        }

                    }
                    catch (Exception ex)
                    {
                        return new(4, ex.Message);
                    }
                default:
                    return new(3, $"station {fIFOStationOutModel.StationName} deosn't support this command");
            }
        }

        public async Task<RequestResult> StationOutBySerialNo(StationOutBySerialNoModel stationOutBySerialNoModel)
        {
            Station? targetStation = GetStationByName(stationOutBySerialNoModel.StationName);
            if (targetStation is null)
            {
                return new(3, $"station {stationOutBySerialNoModel.StationName} not found");
            }
            var check = targetStation.CheckCanRemoveItem();
            if (!check.IsSuccess)
            {
                return check;
            }
            bool isLast = await CheckStationIsLastInProcess(targetStation);
            switch (targetStation.StationType)
            {
                case 12:
                    try
                    {
                        if (targetStation is StationSingleWorkorderMultipleSerials stationSingleWorkorderMultipleSerials)
                        {
                            var item = stationSingleWorkorderMultipleSerials?.RemoveItemDetail(stationOutBySerialNoModel.SerialNo).Obj;
                            //stationSingleWorkorderSingleSerial?.RemoveItemDetail();


                            var taskDetail = stationSingleWorkorderMultipleSerials?.RemoveTaskDetail(item.Id).Obj;
                            taskDetail.FinishedTime = DateTime.Now;
                            await UpsertTaskDetail(taskDetail);
                            //stationSingleWorkorderSingleSerial?.RemoveTaskDetail();


                            

                            if (isLast)
                            {
                                item.FinishedTime = DateTime.Now;
                                if (stationOutBySerialNoModel.Pass)
                                {
                                    item.Okamount++;
                                }
                                else
                                {
                                    item.Ngamount++;
                                }
                                return await UpsertItemDetail(item);
                            }
                            return new(2, $"Station {stationOutBySerialNoModel.StationName} station out by FIFO success");
                        }
                        else
                        {
                            return new(4, $"Station {stationOutBySerialNoModel.StationName} type downcasting error");
                        }

                    }
                    catch (Exception ex)
                    {
                        return new(4, ex.Message);
                    }
                default:
                    return new(3, $"station {stationOutBySerialNoModel.StationName} deosn't support this command");
            }
        }


        public Task<RequestResult> ClearStation(Guid id)
        {
            var target = stations.FirstOrDefault(x => x.Id == id);
            if (target is not null)
            {
                var res = target.ClearWorkorder();
                StationStatechanged();
                return Task.FromResult<RequestResult>(res);
            }
            return Task.FromResult<RequestResult>(new(3, "Station not found"));
        }

        #endregion

        #region workorder
        public async Task<List<Workorder>> GetAllWorkordersConfig()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                Workorder w = new();
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                return await dbContext.Workorders.AsNoTracking().ToListAsync();
            }
        }

        public async Task<List<Workorder>> GetWorkordersRunningConfig()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                Workorder w = new();
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                return await dbContext.Workorders.Where(x => x.Status == 5).AsNoTracking().ToListAsync();
            }
        }
        public async Task<RequestResult> UpsertWorkorderConfig(Workorder workorder)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                    var target = dbContext.Workorders.FirstOrDefault(x => x.Id == workorder.Id);
                    if (target != null)
                    {
                        target.ProcessId = workorder.ProcessId;
                        target.WorkorderNo = workorder.WorkorderNo;
                        target.Lot = workorder.Lot;
                        target.PartNo = workorder.PartNo;
                        target.RecipeCategoryId = workorder.RecipeCategoryId;
                        target.WorkorderRecordCategoryId = workorder.WorkorderRecordCategoryId;
                        target.ItemRecordsCategoryId = workorder.ItemRecordsCategoryId;
                        target.TaskRecordCategoryId = workorder.TaskRecordCategoryId;
                        target.TargetAmount = workorder.TargetAmount;
                    }
                    else
                    {
                        await dbContext.AddAsync(workorder);
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"Upsert workorder {workorder.WorkorderNo}/{workorder.Lot} success");
                }
                catch (Exception e)
                {
                    return new(4, $"Upsert workorder {workorder.WorkorderNo}/{workorder.Lot} fail({e.Message})");
                }

            }
        }
        public async Task<RequestResult> DeleteWorkorder(Workorder workorder)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                    var target = dbContext.Workorders.FirstOrDefault(x => x.Id == workorder.Id);
                    if (target != null)
                    {
                        dbContext.Workorders.Remove(target);
                        await dbContext.SaveChangesAsync();
                        return new(2, $"Delete workorder {workorder.WorkorderNo}/{workorder.Lot} success");
                    }
                    else
                    {
                        return new(4, $"Workorder {workorder.WorkorderNo}/{workorder.Lot} not found");
                    }
                }
                catch (Exception e)
                {
                    return new(4, $"Delete workorder {workorder.WorkorderNo}/{workorder.Lot} fail({e.Message})");
                }


            }
        }
        public async Task<Workorder?> GetWorkorderById(Guid id)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                return await dbContext.Workorders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            }
        }
        public async Task<Workorder?> GetWorkorderByNoAndLot(string no, string lot)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                return await dbContext.Workorders.AsNoTracking().FirstOrDefaultAsync(x => x.WorkorderNo == no && x.Lot == lot);
            }
        }
        public async Task<List<Workorder>> GetRunningWorkordersByProcessID(Guid processId)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                return await dbContext.Workorders.Where(x => x.ProcessId == processId && x.Status == 5)
                    .AsNoTracking().ToListAsync();
            }
        }

        public async Task<RequestResult> StartWorkorderById(Guid id)
        {
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                    var target = await dbContext.Workorders.FirstOrDefaultAsync(x => x.Id == id);
                    if (target is not null)
                    {
                        if (target.CanRun)
                        {
                            target.Start();
                            await dbContext.SaveChangesAsync();
                            return new(2, $"start workorder success");
                        }
                        else
                        {
                            return new(4, "workorder is not allow to run");
                        }
                    }
                    else
                    {
                        return new(4, "Workorder not found");
                    }
                }
            }
            catch (Exception e)
            {
                return new(4, $"start workorder fail({e.Message})");
            }

        }

        public async Task<RequestResult> StopWorkorderById(Guid id)
        {
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                    var target = await dbContext.Workorders.FirstOrDefaultAsync(x => x.Id == id);
                    if (target is not null)
                    {
                        if (target.CanStop)
                        {
                            target.Stop();
                            await dbContext.SaveChangesAsync();
                            return new(2, $"stop workorder success");
                        }
                        else
                        {
                            return new(4, "workorder is not allow to stop");
                        }
                    }
                    else
                    {
                        return new(4, "Workorder not found");
                    }
                }
            }
            catch (Exception e)
            {
                return new(4, $"stop workorder fail({e.Message})");
            }

        }
        #endregion

        #region item

        private async Task<ItemDetail> GetOrGenerateItem(Guid workorderID, string serialNo)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                var targetItem = dbContext.ItemDetails
                    .Include(x => x.ItemRecords)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .FirstOrDefault(x => x.WorkordersId == workorderID && x.SerialNo == serialNo);
                if (targetItem is null)
                {
                    //first item
                    var newItem = await GenerateItemDetail(workorderID, serialNo);
                    return newItem;
                }
                else
                {
                    return targetItem;
                }
            }
        }

        private async Task<ItemDetail> GenerateItemDetail(Guid workorderID, string serialNo)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                var itemDetail = new ItemDetail(workorderID, serialNo);
                await dbContext.ItemDetails.AddAsync(itemDetail);
                await dbContext.SaveChangesAsync();
                return itemDetail;
            }
        }

        private async Task<ItemDetail?> GetItemDetailWithRecord(Guid id)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                return await dbContext.ItemDetails.Include(x => x.ItemRecords).FirstOrDefaultAsync(x => x.Id == id);
            }
        }

        private async Task<RequestResult> UpsertItemDetail(ItemDetail itemDetail)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                    var target = dbContext.ItemDetails.FirstOrDefault(x => x.Id == itemDetail.Id);
                    if (target != null)
                    {
                        target.WorkordersId = itemDetail.WorkordersId;
                        target.SerialNo = itemDetail.SerialNo;
                        target.TargetAmount = itemDetail.TargetAmount;
                        target.Okamount = itemDetail.Okamount;
                        target.Ngamount = itemDetail.Ngamount;
                        target.StartTime = itemDetail.StartTime;
                        target.FinishedTime = itemDetail.FinishedTime;
                    }
                    else
                    {
                        await dbContext.ItemDetails.AddAsync(itemDetail);
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"Upsert task success");
                }
                catch (Exception ex)
                {
                    return new RequestResult(4, $"Upsert task fail({ex.Message})");
                }
            }
        }

        public async Task<List<ItemDetail>> GetItemDetails()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                return await dbContext.ItemDetails.Include(x => x.Workorder)
                    .Include(x=>x.TaskDetails).ThenInclude(x=>x.Station)
                    .Include(x=>x.ItemRecords)
                    .AsNoTracking().ToListAsync();
            }
        }

        #endregion

        #region itemRecord

        public async Task<RequestResult> RecordItemDetail(SingleWorkorderRecordModel singleWorkorderRecordModel)
        {
            var stationSingleWorkorder = stations.OfType<StationSingleWorkorder>();
            var stationSingleWorkorderWithWIP = stationSingleWorkorder.Where(x => x.IsRunning && x.CheckItemIsWIP(singleWorkorderRecordModel.serialNo)); ;
            if (stationSingleWorkorderWithWIP.Any())
            {
                var wo = stationSingleWorkorderWithWIP.FirstOrDefault().Workorder;
                var targetItem = await GetOrGenerateItem(wo.Id, singleWorkorderRecordModel.serialNo);
                var res =  await UpsertItemRecord(targetItem.Id, singleWorkorderRecordModel.recordName, singleWorkorderRecordModel.recordValue);

                var newItemWithRecord = await GetItemDetailWithRecord(targetItem.Id);

                stationSingleWorkorderWithWIP.ToList().ForEach(x => x.RefreshItemAndRecord(newItemWithRecord));



                return res;
            }
            else
            {
                return new(4, $"Item {singleWorkorderRecordModel.serialNo} not found as WIP in any running station");
            }

        }

        private async Task<RequestResult> UpsertItemRecord(Guid itemId, string recordName, string recordValue)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                    var target = await dbContext.ItemRecords.FirstOrDefaultAsync(x => x.ItemId == itemId && x.RecordName == recordName);
                    if (target is not null)
                    {
                        target.RecordValue = recordValue;
                    }
                    else
                    {
                        await dbContext.ItemRecords.AddAsync(new ItemRecord
                        {
                            Id = Guid.NewGuid(),
                            ItemId = itemId,
                            RecordName = recordName,
                            RecordValue = recordValue
                        });
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"Upsert item record success");
                }
                catch (Exception ex)
                {
                    return new RequestResult(4, $"Upsert item record fail({ex.Message})");
                }
            }
        }

        #endregion

        #region task

        private async Task<TaskDetail> GetOrGenerateTask(Guid ItemId, Guid stationID)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                var targetTask = dbContext.TaskDetails
                    .AsNoTracking()
                    .AsSplitQuery()
                    .FirstOrDefault(x => x.StationId == stationID && x.ItemId == ItemId);
                if (targetTask is null)
                {
                    //first item
                    var newTask = await GenerateTaskDetail(ItemId, stationID);
                    //var newTask = await GenerateTaskDetail(newItem.Id, stationID);
                    //newItem.TaskDetails.Add(newTask);
                    return newTask;
                }
                else
                {
                    return targetTask;
                }
            }
        }

        private async Task<TaskDetail> GenerateTaskDetail(Guid itemID, Guid stationID)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                var taskDetail = new TaskDetail(itemID, stationID);
                await dbContext.TaskDetails.AddAsync(taskDetail);
                await dbContext.SaveChangesAsync();
                return taskDetail;
            }
        }

        private async Task<RequestResult> UpsertTaskDetail(TaskDetail taskDetail)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ShopfloorDBContext>();
                    var target = dbContext.TaskDetails.FirstOrDefault(x => x.Id == taskDetail.Id);
                    if (target != null)
                    {
                        target.ItemId = taskDetail.ItemId;
                        target.StationId = taskDetail.StationId;
                        target.StartTime = taskDetail.StartTime;
                        target.FinishedTime = taskDetail.FinishedTime;
                    }
                    else
                    {
                        await dbContext.TaskDetails.AddAsync(taskDetail);
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"Upsert task success");
                }
                catch (Exception ex)
                {
                    return new RequestResult(4, $"Upsert task fail({ex.Message})");
                }
            }
        }

        #endregion
    }
}
