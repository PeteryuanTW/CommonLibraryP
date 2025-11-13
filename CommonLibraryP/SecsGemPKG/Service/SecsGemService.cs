using CommonLibraryP.API;
using CommonLibraryP.MachinePKG;
using DevExpress.Blazor;
using DevExpress.XtraPrinting.Shape.Native;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using QGACTIVEXLib;
using QSACTIVEXLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SecsGemService : IDisposable
    {
        private readonly HSMSQepSetting hsmsQepSetting;
        private readonly IServiceScopeFactory scopeFactory;
        public SecsGemService(IServiceScopeFactory scopeFactory, IOptions<HSMSQepSetting> options)
        {
            this.scopeFactory = scopeFactory;
            hsmsQepSetting = options.Value;
        }


        private QSWrapper qsWrapper = new();
        public QSWrapper QSWrapper => qsWrapper;

        private SECSGemStatus secsGemStatus = new();
        public SECSGemStatus SECSGemStatus => secsGemStatus;

        public List<QSEventLog> QSEventLogs = new List<QSEventLog>();


        private QGWrapper qgWrapper = new();
        string configPath => Path.Combine(AppContext.BaseDirectory, "SECSGEMConfig");




        public event Func<Task>? UIEvent;

        public void UIUpdate()
        {
            if (UIEvent is null) return;

            foreach (var handler in UIEvent.GetInvocationList())
            {
                var func = (Func<Task>)handler;
                _ = Task.Run(func);
            }
        }

        public void Dispose()
        {
            StopHSMS();
        }

        public SECSIParameter GetSECSIParameter()
        {
            if (qsWrapper.lCOMM_Mode == COMMMODE.SECS_MODE)
            {
                return new SECSIParameter()
                {
                    T3 = qsWrapper.T3,
                    DeviceID = qsWrapper.lDeviceID,
                    CommMode = COMMMODE.SECS_MODE,

                    T1 = qsWrapper.T1,
                    T2 = qsWrapper.T2,
                    T4 = qsWrapper.T4,
                    BaudRate = qsWrapper.lBaudRate,
                    COMPort = qsWrapper.lCOMPort,
                    RTY = qsWrapper.RTY,
                    SECS_Connect_Mode = qsWrapper.SECS_Connect_Mode
                };
            }
            else
            {
                return new SECSIParameter
                {
                    CommMode = COMMMODE.SECS_MODE,
                };
            }

        }

        public HSMSSSParameter GetHSMSSSParameter()
        {
            if (qsWrapper.lCOMM_Mode == COMMMODE.HSMS_MODE)
            {
                return new HSMSSSParameter()
                {
                    T3 = qsWrapper.T3,
                    DeviceID = qsWrapper.lDeviceID,
                    CommMode = COMMMODE.HSMS_MODE,

                    T5 = qsWrapper.T5,
                    T6 = qsWrapper.T6,
                    T7 = qsWrapper.T7,
                    T8 = qsWrapper.T8,
                    LinkTestPeriod = qsWrapper.lLinkTestPeriod,
                    LocalIP = qsWrapper.szLocalIP,
                    LocalPort = qsWrapper.nLocalPort,
                    RemoteIP = qsWrapper.szRemoteIP,
                    RemotePort = qsWrapper.nRemotePort,
                    HSMS_Connect_Mode = qsWrapper.HSMS_Connect_Mode,
                };
            }
            else
            {
                return new HSMSSSParameter
                {
                    CommMode = COMMMODE.HSMS_MODE,
                };
            }

        }

        public void SetSetCommonParameter(SECSCommonParameter secsCommonParameter)
        {
            if (secsGemStatus.Hosting)
            {
                StopHSMS();
            }
            if (secsCommonParameter is SECSIParameter secsIParameter)
            {
                SetSECSI(secsIParameter);
            }
            else if (secsCommonParameter is HSMSSSParameter hsmsSSParameter)
            {
                SetHSMS(hsmsSSParameter);
            }
            var res = StartHSMS();
        }

        private void SetCommon(SECSCommonParameter secsCommonParameter)
        {
            qsWrapper.T3 = secsCommonParameter.T3;
            qsWrapper.lDeviceID = secsCommonParameter.DeviceID;
            qsWrapper.lCOMM_Mode = secsCommonParameter.CommMode;
        }
        private void SetSECSI(SECSIParameter sescIParameter)
        {
            SetCommon(sescIParameter);

            qsWrapper.T1 = sescIParameter.T1;
            qsWrapper.T2 = sescIParameter.T2;
            qsWrapper.T4 = sescIParameter.T4;
            qsWrapper.lBaudRate = sescIParameter.BaudRate;
            qsWrapper.lCOMPort = sescIParameter.COMPort;
            qsWrapper.RTY = sescIParameter.RTY;
            qsWrapper.SECS_Connect_Mode = sescIParameter.SECS_Connect_Mode;
        }
        private void SetHSMS(HSMSSSParameter hSMSSSParameter)
        {
            SetCommon(hSMSSSParameter);

            qsWrapper.T5 = hSMSSSParameter.T5;
            qsWrapper.T6 = hSMSSSParameter.T6;
            qsWrapper.T7 = hSMSSSParameter.T7;
            qsWrapper.T8 = hSMSSSParameter.T8;
            qsWrapper.lLinkTestPeriod = hSMSSSParameter.LinkTestPeriod;
            qsWrapper.szLocalIP = hSMSSSParameter.LocalIP;
            qsWrapper.nLocalPort = hSMSSSParameter.LocalPort;
            qsWrapper.szRemoteIP = hSMSSSParameter.RemoteIP;
            qsWrapper.nRemotePort = hSMSSSParameter.RemotePort;
            qsWrapper.HSMS_Connect_Mode = hSMSSSParameter.HSMS_Connect_Mode;


        }

        private void RunOnSTAThread(Action action)
        {
            Exception? exception = null;

            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { exception = ex; }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (exception != null) throw exception;
        }

        public RequestResult InitHSMSFromSetting()
        {
            //common
            qsWrapper.T3 = hsmsQepSetting.T3;
            qsWrapper.lDeviceID = hsmsQepSetting.DeviceID;
            qsWrapper.lCOMM_Mode = COMMMODE.HSMS_MODE;
            //hsms
            qsWrapper.T5 = hsmsQepSetting.T5;
            qsWrapper.T6 = hsmsQepSetting.T6;
            qsWrapper.T7 = hsmsQepSetting.T7;
            qsWrapper.T8 = hsmsQepSetting.T8;
            qsWrapper.lLinkTestPeriod = hsmsQepSetting.LinkTestPeriod;
            qsWrapper.szLocalIP = hsmsQepSetting.LocalIP;
            qsWrapper.nLocalPort = hsmsQepSetting.LocalPort;
            qsWrapper.szRemoteIP = hsmsQepSetting.RemoteIP;
            qsWrapper.nRemotePort = hsmsQepSetting.RemotePort;
            qsWrapper.HSMS_Connect_Mode = HSMS_COMM_MODE.HSMS_PASSIVE_MODE;

            var initRes = qsWrapper.Initialize();
            if (initRes is not 0)
            {
                return new RequestResult(4, $"Init hsms fail({initRes})");
            }
            qsWrapper.QSEvent += new _IQSWrapperEvents_QSEventEventHandler(QSEvent);

            int hsmsPassiveRes = qsWrapper.Start();
            bool success = hsmsPassiveRes is 1;
            secsGemStatus.SetHosting(success);
            UIUpdate();
            if (success)
            {
                return new RequestResult(2, $"Start hsms success");
            }
            else
            {
                return new RequestResult(4, $"Start hsms fail({hsmsPassiveRes})");
            }

        }

        public int InitHSMS()
        {
            var initRes = qsWrapper.Initialize();
            if (initRes is not 0)
            {
                return initRes;
            }
            qsWrapper.QSEvent += new _IQSWrapperEvents_QSEventEventHandler(QSEvent);
            return 0;
        }

        public int StartHSMS()
        {
            var InitRes = InitHSMS();
            if (InitRes is not 0)
            {
                return InitRes;
            }
            if (qsWrapper.lCOMM_Mode == COMMMODE.HSMS_MODE)
            {
                if (qsWrapper.HSMS_Connect_Mode == HSMS_COMM_MODE.HSMS_ACTIVE_MODE)
                {
                    secsGemStatus.SetHosting(false);
                    var hsmsActiveRes = qsWrapper.Start();
                    secsGemStatus.SetConnected(hsmsActiveRes is 1);
                    UIUpdate();
                    return hsmsActiveRes;
                }
                else
                {
                    int hsmsPassiveRes = qsWrapper.Start();
                    secsGemStatus.SetHosting(hsmsPassiveRes is 1);
                    UIUpdate();
                    return hsmsPassiveRes;
                }
            }
            else
            {
                int SECSRes = qsWrapper.Start();
                secsGemStatus.SetHosting(SECSRes is 1);
                secsGemStatus.SetConnected(SECSRes is 1);
                UIUpdate();
                return SECSRes;
            }
        }

        private void QSEvent(int lID, EVENT_ID lMsgID, int S, int F, int W_Bit, int ulSystemBytes, object RawData, object Head, string pEventText)
        {
            var res = SecsParser.Parse(RawData);
            Log(lMsgID, S, F, res);
            switch (lMsgID)
            {
                case EVENT_ID.QS_EVENT_CONNECTED:

                    secsGemStatus.SetConnected(true);
                    break;
                case EVENT_ID.QS_EVENT_RECV_MSG:
                    switch ((S, F))
                    {
                        case (1, 13):
                            //SendMessage(S, F + 1, test);
                            break;
                        default:
                            break;
                    }
                    break;
                case EVENT_ID.QS_EVENT_SEND_MSG:
                    break;
                case EVENT_ID.QS_EVENT_DISCONNECTED:
                    secsGemStatus.SetConnected(false);
                    break;
                default:
                    break;
            }
            UIUpdate();
        }

        public void SendMessage(int S, int F, SecsTreeNode secsTreeNode)
        {
            int systemBytes = 0;
            var secsData = SecsParser.EncodeItem(secsTreeNode);
            RunOnSTAThread(() =>
            {
                qsWrapper.SendSECSIIMessage(S, F, 1, ref systemBytes, secsData);
            });
        }

        private void Log(EVENT_ID eventType, int s, int f, SecsTreeNode? SecsItem = null)
        {
            QSEventLogs.Add(new QSEventLog()
            {
                EventType = eventType,
                S = s,
                F = f,
                LogTime = DateTime.Now,
                SecsItem = SecsItem
            });
            UIUpdate();
        }

        public RequestResult StopHSMS()
        {
            qsWrapper.QSEvent -= new QSACTIVEXLib._IQSWrapperEvents_QSEventEventHandler(QSEvent);
            var stopRes = qsWrapper.Stop();
            bool success = stopRes is 1;
            secsGemStatus.SetHosting(!success);
            UIUpdate();
            if (success)
            {
                return new RequestResult(2, $"Stop hsms success");

            }
            else
            {
                return new RequestResult(4, $"Stop hsms fail({stopRes})");
            }
        }


        public int InitGem()
        {
            var qgRes = qgWrapper.Initialize(configPath);
            if (qgRes is not 0)
            {
                qgWrapper.QGEvent += new _IQGWrapperEvents_QGEventEventHandler(qgEvent);
                qgWrapper.PPEvent += new _IQGWrapperEvents_PPEventEventHandler(qgInfoEvent);
                qgWrapper.TerminalMsgReceive += new _IQGWrapperEvents_TerminalMsgReceiveEventHandler(qgTerminalMsgReceive);
                return qgRes;
            }
            return 0;
        }

        private void qgEvent(int lID, int S, int F, int W_Bit, int SystemBytes, object RawData, int Length)
        {
            qsWrapper.SendSECSIIMessage(S, F, W_Bit, ref SystemBytes, RawData);
        }

        private void qgInfoEvent(PP_TYPE MsgID, string InfoData)
        {

        }

        private void qgTerminalMsgReceive(string Message)
        {

        }


        #region secsgem items

        public async Task<List<SecsTreeNode>> GetAllSecsGemRootItems()
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SecsGemDBContext>();
                return await dbContext.SecsTreeNodes.AsNoTracking().Where(i => i.ParentId == null).ToListAsync();
            }
        }

        public async Task<List<SecsTreeNode>> GetAllFlatSecsGemItems(Guid parentId)
        {
            var result = new List<SecsTreeNode>();
            var queue = new Queue<Guid>();
            queue.Enqueue(parentId);

            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SecsGemDBContext>();
            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var current = await dbContext.SecsTreeNodes
                    .FirstOrDefaultAsync(x => x.Id == currentId);
                if (current is null)
                {
                    return result;
                }
                result.Add(await GetSecsGemItemValue(dbContext, current.Id));


                var children = await dbContext.SecsTreeNodes
                    .Where(x => x.ParentId == currentId)
                    .ToListAsync();

                foreach (var child in children)
                {
                    queue.Enqueue(child.Id);
                }
            }

            return result;
        }


        private async Task<SecsTreeNode?> GetSecsGemItemValue(SecsGemDBContext dbContext, Guid id)
        {
            return await dbContext.SecsTreeNodes.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
            //switch (res)
            //{
            //    case SecsBinary binary:
            //        return await dbContext.SecsBinarys
            //            .Include(x => x.BinaryValues)
            //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
            //    case SecsBool boolean:
            //        return await dbContext.SecsBools
            //            .Include(x => x.BoolValues)
            //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);

            //    case SecsI1 i1:
            //        return await dbContext.SecsI1s
            //            .Include(x => x.SbyteValues)
            //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
            //    case SecsI2 i2:
            //        return await dbContext.SecsI2s
            //            .Include(x => x.ShortValues)
            //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
            //    case SecsI4 i4:
            //        return await dbContext.SecsI4s
            //            .Include(x => x.IntValues)
            //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
            //    case SecsI8 i8:
            //        return await dbContext.SecsI8s
            //            .Include(x => x.LongValues)
            //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);

            //    case SecsU1 u1:
            //        return await dbContext.SecsU1s
            //            .Include(x => x.ByteValues)
            //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
            //    case SecsU2 u2:
            //        return await dbContext.SecsU2s
            //            .Include(x => x.UshortValues)
            //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
            //    case SecsU4 u4:
            //        return await dbContext.SecsU4s
            //            .Include(x => x.UintValues)
            //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
            //    case SecsU8 u8:
            //        return await dbContext.SecsU8s
            //            .Include(x => x.UlongValues)
            //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);

            //    case SecsF4 f4:
            //        return await dbContext.SecsF4s
            //            .Include(x => x.FloatValues)
            //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
            //    case SecsF8 f8:
            //        return await dbContext.SecsF8s
            //            .Include(x => x.DoubleValues)
            //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
            //    default:
            //        return res;
            //}
        } 

        public async Task<SecsTreeNode?> GetSecsGemItemById(Guid id)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SecsGemDBContext>();
                var res = await dbContext.SecsTreeNodes.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
                if (res is SecsList secsList)
                {
                    var childrenId = await GetSecsGemItemChildrenId(dbContext, secsList.Id);
                    var children = await Task.WhenAll(childrenId.Select(x => GetSecsGemItemById(x)));
                    secsList.ChildrenNode = [.. children];
                    return secsList;
                }
                else
                {
                    return res;
                    //switch (res)
                    //{
                    //    case SecsBinary binary:
                    //        return await dbContext.SecsBinarys
                    //            .Include(x => x.BinaryValues)
                    //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
                    //    case SecsBool boolean:
                    //        return await dbContext.SecsBools
                    //            .Include(x => x.BoolValues)
                    //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);

                    //    case SecsI1 i1:
                    //        return await dbContext.SecsI1s
                    //            .Include(x => x.SbyteValues)
                    //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
                    //    case SecsI2 i2:
                    //        return await dbContext.SecsI2s
                    //            .Include(x => x.ShortValues)
                    //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
                    //    case SecsI4 i4:
                    //        return await dbContext.SecsI4s
                    //            .Include(x => x.IntValues)
                    //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
                    //    case SecsI8 i8:
                    //        return await dbContext.SecsI8s
                    //            .Include(x => x.LongValues)
                    //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);

                    //    case SecsU1 u1:
                    //        return await dbContext.SecsU1s
                    //            .Include(x => x.ByteValues)
                    //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
                    //    case SecsU2 u2:
                    //        return await dbContext.SecsU2s
                    //            .Include(x => x.UshortValues)
                    //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
                    //    case SecsU4 u4:
                    //        return await dbContext.SecsU4s
                    //            .Include(x => x.UintValues)
                    //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
                    //    case SecsU8 u8:
                    //        return await dbContext.SecsU8s
                    //            .Include(x => x.UlongValues)
                    //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);

                    //    case SecsF4 f4:
                    //        return await dbContext.SecsF4s
                    //            .Include(x => x.FloatValues)
                    //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
                    //    case SecsF8 f8:
                    //        return await dbContext.SecsF8s
                    //            .Include(x => x.DoubleValues)
                    //            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == res.Id);
                    //    default:
                    //        return res;
                    //}
                }
            }
        }

        private async Task<List<Guid>> GetSecsGemItemChildrenId(SecsGemDBContext secsGemDBContext ,Guid parentId)
        {
             return await secsGemDBContext.SecsTreeNodes.AsNoTracking().Where(i => i.ParentId == parentId).Select(x => x.Id).ToListAsync();
             
        }




        #endregion
    }
}
