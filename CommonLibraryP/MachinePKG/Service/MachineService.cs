using CommonLibraryP.API;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CommonLibraryP.Data;
using System.Net;
using static System.Formats.Asn1.AsnWriter;
using System.Runtime.CompilerServices;

namespace CommonLibraryP.MachinePKG
{
    public class MachineService
    {
        private readonly IServiceScopeFactory scopeFactory;
        public MachineService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
            //IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            //var addr = ipEntry.AddressList.Where(x=>x.AddressFamily== System.Net.Sockets.AddressFamily.InterNetwork);
        }

        #region modbus slave

        private List<ModbusSlaveConfig> modbusSlaves = new();
        public List<ModbusSlaveConfig> ModbusSlaves => modbusSlaves;
        public async Task InitAllModbusSlaves()
        {
            modbusSlaves = await GetAllModbusSlaveConfigs();
            foreach (var slave in modbusSlaves)
            {
                try
                {
                    //await slave.Init();
                }
                catch (Exception e)
                {

                }
            }
        }
        public Task<List<ModbusSlaveConfig>> GetAllModbusSlaveConfigs()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                return Task.FromResult(dbContext.ModbusSlaveConfigs.AsNoTracking().ToList());
            }
        }
        public async Task<RequestResult> UpsertMudbusConfig(ModbusSlaveConfig modbusSlaveConfig)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                    var target = dbContext.ModbusSlaveConfigs.FirstOrDefault(x => x.Id == modbusSlaveConfig.Id);
                    bool exist = target is not null;
                    if (exist)
                    {
                        target.Ip = modbusSlaveConfig.Ip;
                        target.Port = modbusSlaveConfig.Port;
                        target.Station = modbusSlaveConfig.Station;
                    }
                    else
                    {
                        await dbContext.ModbusSlaveConfigs.AddAsync(modbusSlaveConfig);
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"upsert modbus slave {modbusSlaveConfig.Ip} success");
                }
                catch (Exception e)
                {
                    return new(4, $"upsert modbus slave {modbusSlaveConfig.Ip} fail({e.Message})");
                }

            }
        }

        #endregion

        #region machine
        private List<Machine> machines = new();

        //public Action<Guid, DataEditMode>? MachineConfigChangedAct { get; set; }

        public List<Machine> Machines => machines;

        public Task<List<Machine>> GetAllMachinesConfig()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                return Task.FromResult(dbContext.Machines.AsNoTracking().ToList());
            }
        }

        public async Task InitAllMachinesFromDB()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                var tmp = dbContext.Machines.Include(x => x.TagCategory)
                    .ThenInclude(x => x.Tags)
                    .ThenInclude(x => x.TagWarningConditions)
                    .AsSplitQuery()
                    .AsNoTracking()
                    .ToList();
                machines = tmp.Select(x => InitMachineToDerivesClass(x)).ToList();
                List<Task> tasks = new();
                foreach (Machine machine in machines)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        machine.InitMachine();
                        if (machine.Enabled)
                        {
                            await machine.StartUpdating();
                        }
                    }));

                }
                await Task.WhenAll(tasks);
            }
        }

        public async Task<Machine?> InitMachineFromDBById(Guid id)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                var tmp = await dbContext.Machines.Include(x => x.TagCategory).ThenInclude(x => x.Tags).ThenInclude(x=>x.TagWarningConditions)
                    .AsSplitQuery()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);
                tmp = InitMachineToDerivesClass(tmp);
                tmp.InitMachine();
                if (tmp.Enabled)
                {
                    Task.Run(async () => await tmp.StartUpdating());
                }
                return tmp;
            }
        }

        public async Task<RequestResult> UpsertMachineConfig(Machine machine)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                    var target = dbContext.Machines.FirstOrDefault(x => x.Id == machine.Id);
                    bool exist = target is not null;
                    if (exist)
                    {
                        target.Name = machine.Name;
                        target.Ip = machine.Ip;
                        target.Port = machine.Port;
                        target.ConnectionType = machine.ConnectionType;
                        target.MaxRetryCount = machine.MaxRetryCount;
                        target.TagCategoryId = machine.TagCategoryId;
                        target.Enabled = machine.Enabled;
                        target.UpdateDelay = machine.UpdateDelay;
                        target.RecordStatusChanged = machine.RecordStatusChanged;
                    }
                    else
                    {
                        await dbContext.Machines.AddAsync(machine);
                    }
                    await dbContext.SaveChangesAsync();

                    if (exist)
                    {
                        await RemoveMachineFromList(machine);
                    }
                    var newMachine = await InitMachineFromDBById(machine.Id);
                    await AddMachineToList(newMachine);
                    return new(2, $"upsert machine {machine.Name} success");
                }
                catch (Exception e)
                {
                    return new(4, $"upsert machine {machine.Name} fail({e.Message})");
                }

            }
        }

        public async Task<RequestResult> DeleteMachine(Machine machine)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                    var target = dbContext.Machines.FirstOrDefault(x => x.Id == machine.Id);
                    if (target != null)
                    {
                        dbContext.Remove(target);
                        await dbContext.SaveChangesAsync();
                        await RemoveMachineFromList(target);
                        return new(2, $"Delete machine {machine.Name} success");
                    }
                    else
                    {
                        return new(4, $"Machine {machine.Name} not found");
                    }

                }
                catch (Exception e)
                {
                    return new(4, $"Delete machine {machine.Name} fail({e.Message})");
                }

            }
        }

        public Task<Machine?> GetMachineByIDAsync(Guid? id)
        {
            return Task.FromResult(machines.FirstOrDefault(x => x.Id == id));
        }

        public Machine? GetMachineByID(Guid? id)
        {
            return machines.FirstOrDefault(x => x.Id == id);
        }

        public Task<Machine?> GetMachineByName(string name)
        {
            return Task.FromResult(machines.FirstOrDefault(x => x.Name == name));
        }

        public virtual Machine InitMachineToDerivesClass(Machine machine)
        {
            var targetMachineType = MachineTypeEnumHelper.GetConnectionTypeWrapperClassByIndex(machine.ConnectionType);
            if (targetMachineType is not null)
            {
                Machine res = Activator.CreateInstance(targetMachineType.Type) as Machine;
                if (res is not null)
                {
                    CopyMachineAttributes(res, machine);
                    res.MachineStatechangedRecordAct += MachineStatusChangedRecord;
                    return res;
                }
                else
                {
                    throw new Exception($"Machine type error");
                }
            }
            throw new Exception($"Machine type not insert");
        }

        private void CopyMachineAttributes(Machine childMachine, Machine parentMachine)
        {
            foreach (var prop in typeof(Machine).GetProperties())
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    prop.SetValue(childMachine, prop.GetValue(parentMachine));
                }
            }

        }

        //public void MachineConfigChanged(Guid id, DataEditMode mode)
        //{
        //    MachineConfigChangedAct?.Invoke(id, mode);
        //}

        //public async Task RefreshMachine(Machine machine, DataEditMode dataEditMode)
        //{
        //    var target = await GetMachineByIDAsync(machine.Id);
        //    if (target != null)
        //    {
        //        //update or delete


        //        if (dataEditMode != DataEditMode.Delete)
        //        {
        //            machines.Add(await InitMachineFromDBById(machine.Id));
        //        }
        //        else
        //        {
        //        }
        //    }
        //    else
        //    {
        //        machines.Add(await InitMachineFromDBById(machine.Id));
        //    }
        //    MachineConfigChanged(machine.Id, dataEditMode);
        //}
        private async Task RemoveMachineFromList(Machine machine)
        {
            var target = await GetMachineByIDAsync(machine.Id);
            if (target is not null)
            {
                target.MachineStatechangedRecordAct -= MachineStatusChangedRecord;
                machines.Remove(target);
                target.Dispose();
            }
        }

        private async Task AddMachineToList(Machine machine)
        {
            var target = await GetMachineByIDAsync(machine.Id);
            if (target is null)
            {
                machines.Add(machine);
            }
        }



        public async Task MachineStatusChangedRecord(Machine machine, MachineStatusRecordType machineStatusRecordType)
        {
            if (!machine.RecordStatusChanged)
            {
                return;
            }
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                    var newRocord = new MachineStatusLog
                    {
                        Id = Guid.NewGuid(),
                        MachineID = machine.Id,
                        Status = machine.StatusCode,
                        LogTime = DateTime.Now,
                    };
                    await dbContext.MachineStatusLogs.AddAsync(newRocord);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {

            }
        }

        public RequestResult RegisterTagValueChange(string machineName, string tagName, Func<Tag, Task> tagListener)
        {
            var targetMachine = machines.FirstOrDefault(x => x.Name == machineName);
            if (targetMachine is null)
            {
                return new(4, $"Machine {machineName} not found");
            }
            var targetTag = targetMachine.TagCategory?.Tags.FirstOrDefault(x => x.Name == tagName);
            if (targetTag is null)
            {
                return new(4, $"Tag {tagName} not found in machine {machineName}");
            }
            targetTag.TagValueChanged += tagListener;
            return new(2, $"Listen machine {machineName} tag {tagName} value change success");
        }

        #endregion

        #region utilization

        public Task<List<MachineStatusLog>> GetMachineStatusLogByID(MachineUtilizationDTO machineUtilizationDTO)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                return Task.FromResult(dbContext.MachineStatusLogs.Where(x => x.MachineID == machineUtilizationDTO.MachineID && x.LogTime >= machineUtilizationDTO.Start && x.LogTime <= machineUtilizationDTO.End).OrderBy(x => x.LogTime).AsNoTracking().ToList());
            }
        }

        public async IAsyncEnumerable<MachineStatusInterval> CalculateMachineStatusIntervalByOrderedLog(List<MachineStatusLog> machineStatusLogs, ushort delayMilliSec, IProgress<int>? progress)
        {
            int totalCount = machineStatusLogs.Count();
            progress?.Report(0);
            for (int i = 0; i < totalCount; i++)
            {
                if (i == totalCount - 1)
                {
                    //res.Add(new(machineStatusLogs[i].LogTime, DateTime.Now, (Status)machineStatusLogs[i].Status));
                    yield return new(machineStatusLogs[i].LogTime, DateTime.Now, machineStatusLogs[i].Status);
                }
                else
                {
                    //res.Add(new(machineStatusLogs[i].LogTime, machineStatusLogs[i + 1].LogTime, (Status)machineStatusLogs[i].Status));
                    yield return new(machineStatusLogs[i].LogTime, machineStatusLogs[i + 1].LogTime, machineStatusLogs[i].Status);
                }
                await Task.Delay(delayMilliSec);
                progress?.Report(i * 100 / totalCount);
            }
        }

        public async IAsyncEnumerable<MachineStatusInterval> CalculateStatusIntervalsAsyncStream(List<MachineStatusLog> logs, IProgress<int>? progress = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (logs == null || logs.Count == 0)
                yield break;

            var orderedLogs = logs.OrderBy(log => log.LogTime).ToList();

            DateTime currentStart = orderedLogs[0].LogTime;
            int currentStatus = orderedLogs[0].Status;

            int total = orderedLogs.Count;

            for (int i = 1; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentLog = orderedLogs[i];

                if (currentLog.Status != currentStatus)
                {
                    yield return new MachineStatusInterval(currentStart, currentLog.LogTime, currentStatus);
                    currentStart = currentLog.LogTime;
                    currentStatus = currentLog.Status;
                }

                // 模擬非同步處理
                await Task.Yield();

                // 回報進度（整數百分比）
                int percent = (int)((i / (double)total) * 100);
                progress?.Report(percent);
            }

            // 最後一段
            yield return new MachineStatusInterval(currentStart, DateTime.Now, currentStatus);
            progress?.Report(100);
        }



        public Task<RequestResult> ClearMachineStatusLogBeforeSpecificTime(DateTime? time)
        {
            var t = time is null ? DateTime.Now : time.Value;
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                    var targets = dbContext.MachineStatusLogs.Where(x => x.LogTime < t);
                    if (targets.Count() > 0)
                    {
                        dbContext.MachineStatusLogs.RemoveRange(targets);
                        dbContext.SaveChanges();
                        return Task.FromResult(new RequestResult(2, $"Clear machine status log before {t} success"));
                    }
                    else
                    {
                        return Task.FromResult(new RequestResult(1, $"No machine status logs before {t}"));
                    }
                }
                catch (Exception ex)
                {
                    return Task.FromResult(new RequestResult(4, ex.Message));
                }
            }
        }

        #endregion

        #region tag
        public Task<List<TagCategory>> GetAllTagCategories()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                return Task.FromResult(dbContext.TagCategories.AsNoTracking().ToList());
            }
        }

        public Task<List<TagCategory>> GetAllTagCategoriesWithTags()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                return Task.FromResult(dbContext.TagCategories.Include(x => x.Tags).ThenInclude(x => x.TagWarningConditions).AsNoTracking().ToList());
            }
        }

        public async Task<List<TagCategory>> GetAllSuitableTags(int index)
        {
            var suitableCat = MachineTypeEnumHelper.GetSuitableConnectionTypeWrapperClasses(index).ToList();
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                var tmp = await dbContext.TagCategories.AsNoTracking().ToListAsync();
                var res = tmp.Where(x => suitableCat.Exists(y => y.Index == x.ConnectionType)).ToList();
                return res;
            }
        }

        public List<Tag> GetTagsByCatId(Guid? catID)
        {
            if (catID is null)
            {
                return new List<Tag>();
            }
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                var targetCat = dbContext.TagCategories.Include(x => x.Tags).AsNoTracking().FirstOrDefault(x => x.Id == catID);
                if (targetCat is not null)
                {
                    return targetCat.Tags.ToList();
                }
                else
                {
                    return new List<Tag>();
                }
            }
        }

        public int GetTagTypeCodeByIds(Guid? catID, Guid? tagID)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                var targetTag = dbContext.Tags.FirstOrDefault(x => x.CategoryId == catID && x.Id == tagID);
                return targetTag is null ? 0 : targetTag.DataType;
            }
        }

        public Task<List<TagCategory>> GetCategoryByConnectionType(int connectionType)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                return Task.FromResult(dbContext.TagCategories.Where(x => x.ConnectionType == connectionType).ToList());
            }
        }

        public async Task<RequestResult> UpsertTagCategory(TagCategory tagCategory)
        {
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                    var targetTagCat = dbContext.TagCategories.FirstOrDefault(x => x.Id == tagCategory.Id);
                    if (targetTagCat != null)
                    {
                        targetTagCat.Name = tagCategory.Name;
                        targetTagCat.ConnectionType = tagCategory.ConnectionType;
                    }
                    else
                    {
                        await dbContext.TagCategories.AddAsync(tagCategory);
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"Upsert tag category {tagCategory.Name} success");
                }
            }
            catch (Exception ex)
            {
                return new(4, ex.Message);
            }
        }

        public async Task<RequestResult> DeleteTagCategory(TagCategory tagCategory)
        {
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                    var targetTagCat = dbContext.TagCategories.Include(x => x.Tags).FirstOrDefault(x => x.Id == tagCategory.Id);
                    if (targetTagCat != null)
                    {
                        dbContext.TagCategories.Remove(targetTagCat);
                        await dbContext.SaveChangesAsync();
                        return new(2, $"Delete tag category {targetTagCat.Name} success");
                    }
                    else
                    {
                        return new(4, $"Tag category {targetTagCat.Name} not found");
                    }

                }
            }
            catch (Exception ex)
            {
                return new(4, ex.Message);
            }
        }

        public async Task<RequestResult> UpsertTagTPC(Tag tag)
        {
            if (tag is ModbusTCPTag modbusTCPTag)
            {
                return await UpsertTag<ModbusTCPTag>(modbusTCPTag);
            }
            else
            {
                return new(4, $"upsert tag {tag.Name} fail(downcasting fail)");
            }
        }

        private async Task<RequestResult> UpsertTag<T>(T newTag) where T : Tag
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                    var targetTag = await dbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == newTag.Id);
                    if (targetTag is not null)
                    {
                        dbContext.Entry<T>(targetTag).CurrentValues.SetValues(newTag);
                    }
                    else
                    {
                        dbContext.Set<T>().Add(newTag);
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"upsert tag {newTag.Name} success");
                }
                catch (Exception e)
                {
                    return new(4, $"upsert tag {newTag.Name} fail({e.Message})");
                }

            }
        }

        public async Task<RequestResult> DeleteTagTCP(Tag targetTag)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                if (targetTag is ModbusTCPTag modbusTCPTag)
                {
                    return await DeleteTag<ModbusTCPTag>(modbusTCPTag);
                }
                else
                {
                    return new(4, $"delete tag {targetTag.Name} fail(downcasting fail)");
                }
            }
        }

        private async Task<RequestResult> DeleteTag<T>(T targetTag) where T : Tag
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                    var existingNode = await dbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == targetTag.Id);
                    if (existingNode is not null)
                    {
                        dbContext.Entry(existingNode).State = EntityState.Deleted;
                    }
                    else
                    {
                        return new(4, $"tag {targetTag.Name} not found");
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"delete tag {targetTag.Name} success");
                }
                catch (Exception e)
                {
                    return new(4, $"delete tag {targetTag.Name} fail({e.Message})");
                }
            }
        }

        public async Task<Tag?> GetMachineTag(string machineName, string tagName)
        {
            Machine? targetMachine = await GetMachineByName(machineName);
            if (targetMachine != null)
            {
                if (targetMachine.HasCategory)
                {
                    Tag? targetTag = targetMachine.TagCategory.Tags.FirstOrDefault(x => x.Name == tagName);
                    if (targetTag != null)
                    {
                        if (!targetTag.UpdateByTime)
                        {
                            await targetMachine.UpdateTag(targetTag);
                        }
                        return targetTag;
                    }
                }
            }
            return null;
        }

        public Tag? GetMachineTagById(Guid machineId, Guid tagId)
        {
            Machine? targetMachine = GetMachineByID(machineId);
            if (targetMachine != null)
            {
                if (targetMachine.HasCategory)
                {
                    Tag? targetTag = targetMachine.TagCategory.Tags.FirstOrDefault(x => x.Id == tagId);
                    if (targetTag != null)
                    {
                        return targetTag;
                    }
                }
            }
            return null;
        }

        //main set function
        private async Task<RequestResult> SetMachineTag(string machineName, string tagName, object val)
        {
            Machine? targetMachine = await GetMachineByName(machineName);

            if (targetMachine != null)
            {
                if (!targetMachine.RunFlag)
                {
                    return new(4, $"Machine {machineName} status {targetMachine.StatusStr} is not avaulable now");
                }
                if (targetMachine.HasCategory)
                {
                    Tag? targetTag = targetMachine.TagCategory.Tags.FirstOrDefault(x => x.Name == tagName);
                    if (targetTag != null)
                    {
                        return await targetMachine.SetTag(targetTag.Name, val);
                    }
                    else
                    {
                        return new(4, $"Tag {tagName} not found in machine {machineName}");
                    }
                }
                else
                {
                    return new(4, $"Machine tag category not set");
                }
            }
            else
            {
                return new(4, $"Machine {machineName} not found");
            }
        }

        public async Task<RequestResult> SetMachineTagByString(string machineName, string tagName, string valString)
        {
            Machine? targetMachine = await GetMachineByName(machineName);
            if (targetMachine != null)
            {
                if (!targetMachine.RunFlag)
                {
                    return new(4, $"Machine {machineName} status {targetMachine.StatusStr} is not avaulable now");
                }
                if (targetMachine.HasCategory)
                {
                    Tag? targetTag = targetMachine.TagCategory.Tags.FirstOrDefault(x => x.Name == tagName);
                    if (targetTag != null)
                    {
                        try
                        {
                            switch (targetTag.DataType)
                            {
                                case 1:
                                    bool boolVal;
                                    bool boolParseRes = bool.TryParse(valString, out boolVal);
                                    if (boolParseRes)
                                    {
                                        return await SetMachineTag(machineName, targetTag.Name, boolVal);
                                    }
                                    else
                                    {
                                        return new(4, $"Set tag {targetTag.Name} fail(parsing {valString} to bool fail)");
                                    }
                                case 2:
                                    ushort ushortVal;
                                    bool ushortParseRes = ushort.TryParse(valString, out ushortVal);
                                    if (ushortParseRes)
                                    {
                                        return await SetMachineTag(machineName, targetTag.Name, ushortVal);
                                    }
                                    else
                                    {
                                        return new(4, $"Set tag {targetTag.Name} fail(parsing {valString} to ushort fail)");
                                    }
                                case 4:
                                    return await SetMachineTag(machineName, targetTag.Name, valString);
                                case 11:
                                    var boolArrRegexRes = MachineTypeEnumHelper.VerifyValueStringWithDatatype(targetTag.DataType, valString);
                                    if (!boolArrRegexRes)
                                    {
                                        return new(4, $"Boolean array {valString} regular expression invalid");
                                    }
                                    var boolArrVal = ConvertToBooleanArray(valString).ToArray();
                                    return await SetMachineTag(machineName, targetTag.Name, boolArrVal);
                                case 22:
                                    var ushortArrRegexRes = MachineTypeEnumHelper.VerifyValueStringWithDatatype(targetTag.DataType, valString);
                                    if (!ushortArrRegexRes)
                                    {
                                        return new(4, $"Ushort array {valString} regular expression invalid");
                                    }
                                    var ushortArrVal = ConvertToUshortArray(valString).ToArray();
                                    return await SetMachineTag(machineName, targetTag.Name, ushortArrVal);
                                default:
                                    return new(4, $"Set tag {targetTag.Name} fail({((DataType)targetTag.DataType)} not support yet)");
                            }
                        }
                        catch (Exception e)
                        {
                            return new(4, $"Set tag {targetTag.Name} fail({e.Message})");
                        }

                    }
                    else
                    {
                        return new(4, $"Tag {tagName} not found in machine {machineName}");
                    }
                }
                else
                {
                    return new(4, $"Machine tag category not set");
                }
            }
            else
            {
                return new(4, $"Machine {machineName} not found");
            }
        }

        private IEnumerable<bool> ConvertToBooleanArray(string valString)
        {
            string val = valString.Trim('[', ']');
            var boolArr = val.Split(",");
            foreach (var boolString in boolArr)
            {
                bool boolVal;
                bool boolParseRes = bool.TryParse(boolString, out boolVal);
                if (boolParseRes)
                {
                    yield return boolVal;
                }
            }

        }
        private IEnumerable<ushort> ConvertToUshortArray(string valString)
        {
            string val = valString.Trim('[', ']');
            var ushortArr = val.Split(",");
            foreach (var ushortString in ushortArr)
            {
                ushort ushortVal;
                bool ushortParseRes = ushort.TryParse(ushortString, out ushortVal);
                if (ushortParseRes)
                {
                    yield return ushortVal;
                }
            }
        }

        public async Task<RequestResult> SetMachineTagByIdAndString(Guid? machineId, Guid? tagId, string valString)
        {
            Machine? targetMachine = GetMachineByID(machineId);
            if (targetMachine != null)
            {
                if (targetMachine.HasCategory)
                {
                    Tag? targetTag = targetMachine.TagCategory.Tags.FirstOrDefault(x => x.Id == tagId);
                    if (targetTag != null)
                    {
                        return await SetMachineTagByString(targetMachine.Name, targetTag.Name, valString);
                    }
                    else
                    {
                        return new(4, $"Tag not found in machine");
                    }
                }
                else
                {
                    return new(4, $"Machine tag category not set");
                }
            }
            else
            {
                return new(4, $"Machine not found");
            }
        }




        #endregion

        #region tag warning condition
        public async Task<RequestResult> UpsertTagWarningConditionTPC(TagWarningCondition tagWarningCondition)
        {
            switch (tagWarningCondition)
            {
                case TagWarningUshortCondition tagWarningUshortCondition:
                    return await UpsertTagWarningCondition<TagWarningUshortCondition>(tagWarningUshortCondition);
                case TagWarningBoolCondition tagWarningBoolCondition:
                    return await UpsertTagWarningCondition<TagWarningBoolCondition>(tagWarningBoolCondition);
                default:
                    return new(4, $"upsert tag warning condition {tagWarningCondition.Name} fail(downcasting fail)");
            }
        }
        private async Task<RequestResult> UpsertTagWarningCondition<T>(T newWarningCondition) where T : TagWarningCondition
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                    var targetWarningCondition = await dbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == newWarningCondition.Id);
                    if (targetWarningCondition is not null)
                    {
                        dbContext.Entry<T>(targetWarningCondition).CurrentValues.SetValues(newWarningCondition);
                    }
                    else
                    {
                        dbContext.Set<T>().Add(newWarningCondition);
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"upsert tag warning condition {newWarningCondition.Name} success");
                }
                catch (Exception e)
                {
                    return new(4, $"upsert tag warning condition {newWarningCondition.Name} fail({e.Message})");
                }

            }
        }


        public async Task<RequestResult> DeleteTagWarningConditionTCP(TagWarningCondition targetTagWarningCondition)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                switch (targetTagWarningCondition)
                {
                    case TagWarningUshortCondition tagWarningUshortCondition:
                        return await DeleteTagWarningCondition<TagWarningUshortCondition>(tagWarningUshortCondition);
                    case TagWarningBoolCondition tagWarningBoolCondition:
                        return await DeleteTagWarningCondition<TagWarningBoolCondition>(tagWarningBoolCondition);
                    default:
                        return new(4, $"delete tag warning condition {targetTagWarningCondition.Name} fail(downcasting fail)");
                }
            }
        }

        private async Task<RequestResult> DeleteTagWarningCondition<T>(T targetTagWarningCondition) where T : TagWarningCondition
        {
            using (var scope = scopeFactory.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                    var existingNode = await dbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == targetTagWarningCondition.Id);
                    if (existingNode is not null)
                    {
                        dbContext.Entry(existingNode).State = EntityState.Deleted;
                    }
                    else
                    {
                        return new(4, $"tag warning condition {targetTagWarningCondition.Name} not found");
                    }
                    await dbContext.SaveChangesAsync();
                    return new(2, $"delete tag warning condition {targetTagWarningCondition.Name} success");
                }
                catch (Exception e)
                {
                    return new(4, $"delete tag warning condition {targetTagWarningCondition.Name} fail({e.Message})");
                }
            }
        }
        #endregion
    }
}
