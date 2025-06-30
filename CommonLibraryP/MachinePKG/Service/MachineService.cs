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

        public Action<Guid, DataEditMode>? MachineConfigChangedAct { get; set; }

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
                var tmp = dbContext.Machines.Include(x => x.TagCategory).ThenInclude(x => x.Tags)
                    .AsSplitQuery()
                    .AsNoTracking()
                    .ToList();
                machines = tmp.Select(x => InitMachineToDerivesClass(x)).ToList();
                List<Task> tasks = new();
                foreach (Machine machine in machines)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        machine.InitMachine();
                        if (machine.Enabled)
                        {
                            machine.StartUpdating();
                        }
                    }));

                }
                await Task.WhenAll(tasks);
            }
        }

        public Machine? InitMachineFromDBById(Guid id)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
                var tmp = dbContext.Machines.Include(x => x.TagCategory).ThenInclude(x => x.Tags)
                    .AsSplitQuery()
                    .AsNoTracking()
                    .FirstOrDefault(x => x.Id == id);
                tmp = InitMachineToDerivesClass(tmp);
                tmp.InitMachine();
                if (tmp.Enabled)
                {
                    tmp.StartUpdating();
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
                        //target.ProcessId = machine.ProcessId;
                        target.Ip = machine.Ip;
                        target.Port = machine.Port;
                        target.ConnectionType = machine.ConnectionType;
                        target.MaxRetryCount = machine.MaxRetryCount;
                        target.TagCategoryId = machine.TagCategoryId;
                        //target.LogicStatusCategoryId = machine.LogicStatusCategoryId;
                        //target.ErrorCodeCategoryId = machine.ErrorCodeCategoryId;
                        target.Enabled = machine.Enabled;
                        target.UpdateDelay = machine.UpdateDelay;
                        target.RecordStatusChanged = machine.RecordStatusChanged;
                    }
                    else
                    {
                        await dbContext.Machines.AddAsync(machine);
                    }
                    await dbContext.SaveChangesAsync();
                    DataEditMode dataEditMode = exist ? DataEditMode.Update : DataEditMode.Insert;
                    await RefreshMachine(machine, dataEditMode);
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
                        await RefreshMachine(target, DataEditMode.Delete);
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

        public void MachineConfigChanged(Guid id, DataEditMode mode)
        {
            MachineConfigChangedAct?.Invoke(id, mode);
        }

        public async Task RefreshMachine(Machine machine, DataEditMode dataEditMode)
        {
            var target = await GetMachineByIDAsync(machine.Id);
            if (target != null)
            {
                //update or delete
                target.MachineStatechangedRecordAct -= MachineStatusChangedRecord;
                machines.Remove(target);
                target.Dispose();

                if (dataEditMode != DataEditMode.Delete)
                {
                    machines.Add(InitMachineFromDBById(machine.Id));
                }
                else
                {
                }
            }
            else
            {
                machines.Add(InitMachineFromDBById(machine.Id));
            }
            MachineConfigChanged(machine.Id, dataEditMode);
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
                return Task.FromResult(dbContext.TagCategories.Include(x => x.Tags).AsNoTracking().ToList());
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
                if (targetMachine.hasCategory)
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
                if (targetMachine.hasCategory)
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
                if (targetMachine.hasCategory)
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
                if (targetMachine.hasCategory)
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
                if (targetMachine.hasCategory)
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

        #region condition

        //private List<Condition> conditions = new();
        //public List<Condition> Conditions => conditions;
        //public async Task InitConditions()
        //{
        //    conditions = await InitAllConditionsHierarchical();
        //    foreach (var condition in conditions)
        //    {
        //        if (condition.Enable)
        //        {
        //            condition.StartMonitorThread(this);
        //        }

        //    }
        //}

        //private async Task<List<Condition>> InitAllConditionsHierarchical()
        //{
        //    using (var scope = scopeFactory.CreateScope())
        //    {
        //        var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
        //        return await dbContext.Conditions.Include(x => x.ConditionNodes.Where(x => x.ParentNodeId == null))
        //            .ThenInclude(x => x.ChildNodes)
        //            .Include(x => x.ConditionActions.OrderBy(x => x.Sequence))
        //            .AsSplitQuery()
        //            .AsNoTracking()
        //            .ToListAsync();
        //    }
        //}

        //private async Task<Condition?> InitConditionsHierarchicalById(Guid id)
        //{
        //    using (var scope = scopeFactory.CreateScope())
        //    {
        //        var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
        //        return await dbContext.Conditions.Include(x => x.ConditionNodes.Where(x => x.ParentNodeId == null))
        //            .ThenInclude(x => x.ChildNodes)
        //            .Include(x => x.ConditionActions.OrderBy(x => x.Sequence))
        //            .AsSplitQuery()
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync(x => x.Id == id);
        //    }
        //}

        //public Condition? GetConditionById(Guid id)
        //    => conditions.FirstOrDefault(x => x.Id == id);

        //public async Task<List<Condition>> GetAllConditionsConfig()
        //{
        //    using (var scope = scopeFactory.CreateScope())
        //    {
        //        var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
        //        return await dbContext.Conditions.Include(x => x.ConditionNodes.Where(x => x.ParentNodeId == null))
        //            .ThenInclude(x => x.ChildNodes)
        //            .Include(x => x.ConditionActions.OrderBy(x => x.Sequence))
        //            .AsSplitQuery()
        //            .AsNoTracking()
        //            .ToListAsync();
        //    }
        //}

        //public async Task<List<ConditionNode>> GetConditionNodesFlatById(Guid conditionId)
        //{
        //    using (var scope = scopeFactory.CreateScope())
        //    {
        //        var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
        //        return await dbContext.ConditionNodes.Where(x => x.ConditionId == conditionId)
        //            .AsNoTracking()
        //            .ToListAsync();
        //    }
        //}

        //public async Task<RequestResult> UpsertCondition(Condition condition)
        //{
        //    using (var scope = scopeFactory.CreateScope())
        //    {
        //        try
        //        {
        //            var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
        //            var target = dbContext.Conditions.FirstOrDefault(x => x.Id == condition.Id);
        //            bool exist = target is not null;
        //            if (exist)
        //            {
        //                target.Name = condition.Name;
        //                target.Enable = condition.Enable;
        //            }
        //            else
        //            {
        //                await dbContext.Conditions.AddAsync(condition);
        //            }
        //            await dbContext.SaveChangesAsync();
        //            //DataEditMode dataEditMode = exist ? DataEditMode.Update : DataEditMode.Insert;
        //            //await RefreshMachine(machine, dataEditMode);
        //            return new(2, $"upsert condition {condition.Name} success");
        //        }
        //        catch (Exception e)
        //        {
        //            return new(4, $"upsert condition {condition.Name} fail({e.Message})");
        //        }

        //    }
        //}
        //public async Task<RequestResult> DeleteCondition(Condition condition)
        //{
        //    using (var scope = scopeFactory.CreateScope())
        //    {
        //        try
        //        {
        //            var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
        //            var target = dbContext.Conditions.FirstOrDefault(x => x.Id == condition.Id);
        //            if (target != null)
        //            {
        //                dbContext.Remove(target);
        //                await dbContext.SaveChangesAsync();
        //                return new(2, $"Delete condition {target.Name} success");
        //            }
        //            else
        //            {
        //                return new(4, $"condition {condition.Name} not found");
        //            }

        //        }
        //        catch (Exception e)
        //        {
        //            return new(4, $"Delete condition {condition.Name} fail({e.Message})");
        //        }

        //    }
        //}

        //public async Task<RequestResult> UpsertConditionNodeTPC(ConditionNode conditionNode)
        //{
        //    if (conditionNode is ConditionLogicNode conditionLogicNode)
        //    {
        //        return await UpsertConditionNode<ConditionLogicNode>(conditionLogicNode);
        //    }
        //    else if (conditionNode is ConditionConstDataNode conditionConstDataNode)
        //    {
        //        return await UpsertConditionNode<ConditionConstDataNode>(conditionConstDataNode);
        //    }
        //    else if (conditionNode is ConditionTagDataNode conditionTagDataNode)
        //    {
        //        return await UpsertConditionNode<ConditionTagDataNode>(conditionTagDataNode);
        //    }
        //    else
        //    {
        //        return new(4, $"upsert condition node {conditionNode.Name} fail(downcasting fail)");
        //    }
        //}

        //private async Task<RequestResult> UpsertConditionNode<T>(T conditionNode) where T : ConditionNode
        //{
        //    using (var scope = scopeFactory.CreateScope())
        //    {
        //        try
        //        {
        //            var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
        //            var existingNode = await dbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == conditionNode.Id);
        //            if (existingNode is not null)
        //            {
        //                dbContext.Entry<T>(existingNode).CurrentValues.SetValues(conditionNode);
        //            }
        //            else
        //            {
        //                dbContext.Set<T>().Add(conditionNode);
        //            }
        //            await dbContext.SaveChangesAsync();
        //            return new(2, $"upsert condition node {conditionNode.Name} success");
        //        }
        //        catch (Exception e)
        //        {
        //            return new(4, $"upsert condition node {conditionNode.Name} fail({e.Message})");
        //        }

        //    }
        //}

        //public async Task<RequestResult> DeleteConditionNodeTCP(ConditionNode conditionNode)
        //{
        //    using (var scope = scopeFactory.CreateScope())
        //    {
        //        if (conditionNode is ConditionLogicNode conditionLogicNode)
        //        {
        //            return await DeleteConditionNode<ConditionLogicNode>(conditionLogicNode);
        //        }
        //        else if (conditionNode is ConditionConstDataNode conditionConstDataNode)
        //        {
        //            return await DeleteConditionNode<ConditionConstDataNode>(conditionConstDataNode);
        //        }
        //        else if (conditionNode is ConditionTagDataNode conditionTagDataNode)
        //        {
        //            return await DeleteConditionNode<ConditionTagDataNode>(conditionTagDataNode);
        //        }
        //        else
        //        {
        //            return new(4, $"delete condition node {conditionNode.Name} fail(downcasting fail)");
        //        }
        //    }
        //}

        //private async Task<RequestResult> DeleteConditionNode<T>(T conditionNode) where T : ConditionNode
        //{
        //    using (var scope = scopeFactory.CreateScope())
        //    {
        //        try
        //        {
        //            var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
        //            var existingNode = await dbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == conditionNode.Id);
        //            if (existingNode is not null)
        //            {
        //                dbContext.Entry(existingNode).State = EntityState.Deleted;
        //            }
        //            else
        //            {
        //                return new(4, $"condition node {conditionNode.Name} not found");
        //            }
        //            await dbContext.SaveChangesAsync();
        //            return new(2, $"delete condition node {conditionNode.Name} success");
        //        }
        //        catch (Exception e)
        //        {
        //            return new(4, $"delete condition node {conditionNode.Name} fail({e.Message})");
        //        }
        //    }
        //}

        //public async Task<RequestResult> UpsertConditionActionTPC(ConditionAction conditionAction)
        //{
        //    if (conditionAction is AwaitAction awaitAction)
        //    {
        //        return await UpsertConditionAction<AwaitAction>(awaitAction);
        //    }
        //    else if (conditionAction is SetTagAction setTagAction)
        //    {
        //        return await UpsertConditionAction<SetTagAction>(setTagAction);
        //    }
        //    else
        //    {
        //        return new(4, $"upsert condition action {conditionAction.Name} fail(downcasting fail)");
        //    }
        //}

        //private async Task<RequestResult> UpsertConditionAction<T>(T conditionAction) where T : ConditionAction
        //{
        //    using (var scope = scopeFactory.CreateScope())
        //    {
        //        try
        //        {

        //            var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
        //            var existingAction = await dbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == conditionAction.Id);
        //            if (existingAction is not null)
        //            {
        //                dbContext.Entry<T>(existingAction).CurrentValues.SetValues(conditionAction);
        //            }
        //            else
        //            {
        //                dbContext.Set<T>().Add(conditionAction);
        //            }
        //            await dbContext.SaveChangesAsync();
        //            return new(2, $"upsert condition action {conditionAction.Name} success");
        //        }
        //        catch (Exception e)
        //        {
        //            return new(4, $"upsert condition action {conditionAction.Name} fail({e.Message})");
        //        }

        //    }
        //}

        //public async Task<RequestResult> DeleteConditionActionTPC(ConditionAction conditionAction)
        //{
        //    using (var scope = scopeFactory.CreateScope())
        //    {
        //        if (conditionAction is AwaitAction awaitAction)
        //        {
        //            return await DeleteConditionAction<AwaitAction>(awaitAction);
        //        }
        //        else if (conditionAction is SetTagAction setTagAction)
        //        {
        //            return await DeleteConditionAction<SetTagAction>(setTagAction);
        //        }
        //        else
        //        {
        //            return new(4, $"delete condition node {conditionAction.Name} fail(downcasting fail)");
        //        }
        //    }
        //}

        //private async Task<RequestResult> DeleteConditionAction<T>(T conditionAction) where T : ConditionAction
        //{
        //    using (var scope = scopeFactory.CreateScope())
        //    {
        //        try
        //        {
        //            var dbContext = scope.ServiceProvider.GetRequiredService<MachineDBContext>();
        //            var existingNode = await dbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == conditionAction.Id);
        //            if (existingNode is not null)
        //            {
        //                dbContext.Entry(existingNode).State = EntityState.Deleted;
        //            }
        //            else
        //            {
        //                return new(4, $"condition node {conditionAction.Name} not found");
        //            }
        //            await dbContext.SaveChangesAsync();
        //            return new(2, $"delete condition node {conditionAction.Name} success");
        //        }
        //        catch (Exception e)
        //        {
        //            return new(4, $"delete condition node {conditionAction.Name} fail({e.Message})");
        //        }
        //    }
        //}

        //public async Task RefreshComdition(Condition condition, DataEditMode dataEditMode)
        //{
        //    var target = GetConditionById(condition.Id);
        //    if (target != null)
        //    {
        //        //update or delete
        //        conditions.Remove(target);
        //        target.StopMonitorThread();

        //        if (dataEditMode != DataEditMode.Delete)
        //        {
        //            var tmp = await InitConditionsHierarchicalById(target.Id);
        //            if (tmp is not null)
        //            {
        //                conditions.Add(tmp);
        //            }
        //        }
        //        else
        //        {
        //        }
        //    }
        //    else
        //    {
        //        var tmp = await InitConditionsHierarchicalById(target.Id);
        //        if (tmp is not null)
        //        {
        //            conditions.Add(tmp);
        //        }
        //    }
        //    //MachineConfigChanged(condition.Id, dataEditMode);
        //}

        #endregion
    }
}
