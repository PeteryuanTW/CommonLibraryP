using System.Net.Sockets;
using CommonLibraryP.API;
using CommonLibraryP.Data;

namespace CommonLibraryP.MachinePKG
{
    public partial class Machine : IDisposable
    {

        protected int retryCount = 0;

        public int RetryCount => retryCount;

        public bool isAutoRetry => retryCount < MaxRetryCount;

        //private PeriodicTimer periodicTimer;
        //private CancellationToken _cts;
        public Machine() { }

        //public Machine(Machine machine)
        //{
        //    Id = machine.Id;
        //    //ProcessId = machine.ProcessId;
        //    Name = machine.Name;
        //    Ip = machine.Ip;
        //    Port = machine.Port;
        //    ConnectionType = machine.ConnectionType;
        //    MaxRetryCount = machine.MaxRetryCount;
        //    Enabled = machine.Enabled;
        //    TagCategoryId = machine.TagCategoryId;
        //    UpdateDelay = machine.UpdateDelay;
        //    RecordStatusChanged = machine.RecordStatusChanged;

        //    if (machine.hasCategory)
        //    {
        //        TagCategory = new TagCategory
        //        {
        //            Id = machine.TagCategory.Id,
        //            Name = machine.TagCategory.Name,
        //            ConnectionType = machine.ConnectionType,

        //            Tags = machine.TagCategory.Tags,
        //        };
        //    }
        //}

        //public Machine(Guid id)
        //{
        //    this.Id = id;
        //}
        public bool hasCategory => TagCategory != null;
        public bool hasTags => hasCategory && TagCategory?.Tags.Count > 0;
        public bool hasTagsUpdateByTime => hasTags && TagCategory.Tags.Any(x => x.UpdateByTime);

        private int statusCode;
        public int StatusCode => statusCode;
        public string StatusStr => CommonEnumHelper.GetStatusDetail(statusCode).DisplayName;

        protected DateTime lastStatusChangedTime;
        protected DateTime lastTagUpdateTime;

        protected virtual bool runFlag => statusCode is not 0 && statusCode is not 1 && statusCode is not 2;
        public bool RunFlag => runFlag;

        public bool canManualRetryFlag => isAutoRetry ? false : statusCode is 2 || statusCode is 8;

        protected string errorMsg = string.Empty;
        public string ErrorMsg => errorMsg;

        public Func<int, Task>? MachineStatuschangedAct;
        public Func<Machine, MachineStatusRecordType, Task>? MachineStatechangedRecordAct;

        protected void MachineStatechanged()
        {
            MachineStatuschangedAct?.Invoke(statusCode);
            if (RecordStatusChanged)
            {
                MachineStatechangedRecordAct?.Invoke(this, MachineStatusRecordType.InputStatus);
            }
        }

        public Func<Task>? TagsStatechangedAct;
        protected void TagsStatechange() => TagsStatechangedAct?.Invoke();

        public void InitMachine()
        {
            statusCode = 0;
            if (hasTags)
            {
                foreach (var item in TagCategory.Tags)
                {
                    item.Init();
                }
            }
            Init();
        }
        public virtual Task ConnectAsync()
        {
            Init();
            return Task.CompletedTask;
        }
        public virtual Task<RequestResult> UpdateTag(Tag tag)
        {
            throw new NotImplementedException();
        }
        public async Task<RequestResult> SetTag(string tagName, object val)
        {
            if (hasCategory)
            {
                Tag tag = TagCategory.Tags.FirstOrDefault(x => x.Name == tagName);
                if (tag != null)
                {
                    return await SetTag(tag, val);
                }
                else
                {
                    return new(4, $"No tag {tagName}");
                }
            }
            else
            {
                return new(4, "No tag exist");
            }
        }
        public virtual Task<RequestResult> SetTag(Tag tag, object val)
        {
            throw new NotImplementedException();
        }

        public virtual Task ManualRun()
        {
            return Task.CompletedTask;
        }

        public virtual Task ManualStop()
        {
            return Task.CompletedTask;
        }
        protected virtual Task UpdateTags()
        {
            lastTagUpdateTime = DateTime.Now;
            return Task.CompletedTask;
        }
        protected virtual Task UpdateStatus()
        {
            return Task.CompletedTask;
        }

        public void StartUpdating()
        {
            try
            {
                new Thread(async () =>
                {
                    while (Enabled)
                    {
                        try
                        {
                            if (runFlag)
                            {
                                await UpdateStatus();
                                if (hasTagsUpdateByTime)
                                {
                                    await UpdateTags();
                                    TagsStatechange();
                                }
                            }
                            else
                            {
                                if (statusCode is 0 || statusCode is 2)
                                {
                                    if (MaxRetryCount is -1)
                                    {
                                        await ConnectAsync();

                                    }
                                    else
                                    {
                                        if (retryCount < MaxRetryCount)
                                        {
                                            await ConnectAsync();
                                        }
                                    }

                                }
                                else if (statusCode is 1)
                                {

                                }
                            }
                        }
                        catch (IOException ex)
                        {
                            Disconnect(ex.Message);
                        }
                        catch (SocketException e)
                        {
                            Disconnect(e.Message);
                        }
                        catch (Exception e)
                        {
                            Error(e.Message);
                        }
                        finally
                        {
                            await Task.Delay(UpdateDelay);
                        }
                    }
                }
                ).Start();

            }
            catch (Exception e)
            {
                Error(e.Message);
            }
        }
        protected void Init()
        {
            statusCode = 0;
            errorMsg = string.Empty;
            lastStatusChangedTime = DateTime.Now;
            MachineStatechanged();
        }
        protected void Idle()
        {
            if (statusCode is not 4)
            {
                statusCode = 4;
                errorMsg = string.Empty;
                lastStatusChangedTime = DateTime.Now;
                MachineStatechanged();
            }
        }
        protected void TryConnecting()
        {
            if (statusCode is not 1)
            {
                statusCode = 1;
                errorMsg = string.Empty;
                lastStatusChangedTime = DateTime.Now;
                MachineStatechanged();
            }
        }
        public void FetchingData()
        {
            if (statusCode is not 3)
            {
                statusCode = 3;
                errorMsg = string.Empty;
                lastStatusChangedTime = DateTime.Now;
                MachineStatechanged();
            }
        }
        public void Running()
        {
            if (statusCode is not 5)
            {
                statusCode = 5;
                errorMsg = string.Empty;
                lastStatusChangedTime = DateTime.Now;
                MachineStatechanged();
            }
        }
        protected void Pause()
        {
            if (statusCode is not 6)
            {
                statusCode = 6;
                errorMsg = string.Empty;
                lastStatusChangedTime = DateTime.Now;
                MachineStatechanged();
            }
        }
        protected void Stop()
        {
            if (statusCode is not 7)
            {
                statusCode = 7;
                lastStatusChangedTime = DateTime.Now;
                MachineStatechanged();
            }
        }
        protected void Disconnect(string msg)
        {
            if (statusCode is not 2)
            {
                statusCode = 2;
                lastStatusChangedTime = DateTime.Now;
                if (!string.IsNullOrEmpty(msg))
                {
                    errorMsg = msg;
                }
                else
                {
                    errorMsg = string.Empty;
                }
                MachineStatechanged();
            }
        }
        protected void Error(string msg)
        {
            if (statusCode is not 8)
            {
                statusCode = 8;
                errorMsg = msg;
                lastStatusChangedTime = DateTime.Now;
                MachineStatechanged();
            }
        }

        protected void SetCustomStatusCode(int CustomStatusCode)
        {

            if (CustomStatusCode <= 100)
            {
                throw new Exception("call reserved status with build in function first");
            }
            if (CustomStatusCode != statusCode)
            {
                statusCode = CustomStatusCode;
                MachineStatechanged();
            }
        }
        //dispose
        public void Dispose()
        {
            Enabled = false;
        }
    }
}
