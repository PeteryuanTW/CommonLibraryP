using CommonLibraryP.API;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using QGACTIVEXLib;
using QSACTIVEXLib;
using System.Runtime.ExceptionServices;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CommonLibraryP.SecsGemPKG
{
	public class SecsGemService : IDisposable
	{
		//private readonly HSMSParameter hsmsParameter;
		private readonly IServiceScopeFactory scopeFactory;
		public SecsGemService(IServiceScopeFactory scopeFactory)
		{
			this.scopeFactory = scopeFactory;
			//hsmsParameter = options.Value;
		}


		private QSWrapper qsWrapper = new();
		public QSWrapper QSWrapper => qsWrapper;

		private SECSStatus secsStatus = new();
		public SECSStatus SECSStatus => secsStatus;

		public List<QSEventLog> QSEventLogs = new List<QSEventLog>();

		public Action? HSMSConfigAct;
		private void HSMSConfigChanged() => HSMSConfigAct?.Invoke();

		private QGWrapper qgWrapper = new();
		public string configPath => AppContext.BaseDirectory;//Path.Combine(AppContext.BaseDirectory, "SECSGEMConfig");

		public GemStatus GemStatus { get; set; } = new();


		public List<SV> SVs = new();

		public event Action? SVsUpdateFunc;

		private void SVUpdate() => SVsUpdateFunc?.Invoke();

		public event Func<int, int, SecsTreeNode, SecsTreeNode, Task>? SecsMessageFunc;

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
		private T RunOnSTAThread<T>(Func<T> func)
		{
			T? result = default;
			Exception? exception = null;

			var thread = new Thread(() =>
			{
				try
				{
					result = func();
				}
				catch (Exception ex)
				{
					exception = ex;
				}
			});

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			thread.Join();

			if (exception != null)
				ExceptionDispatchInfo.Capture(exception).Throw();

			return result!;
		}


		public async Task<RequestResult> UpsertHSMSParameter(HSMSParameter hsmsParameter)
		{
			using (var scope = scopeFactory.CreateScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<SecsGemDBContext>();
				var target = await dbContext.HSMSParameter.FirstOrDefaultAsync(x=>x.Id == 1);
				if (target is null)
				{
					await dbContext.HSMSParameter.AddAsync(hsmsParameter);
				}
				else
				{
					dbContext.Entry(target).CurrentValues.SetValues(hsmsParameter);
				}
				await dbContext.SaveChangesAsync();
				return new(2, "Upsert hsms config success");
			}
		}

		public async Task<HSMSParameter?> GetHSMSParameter()
		{
			using (var scope = scopeFactory.CreateScope())
			{
				var hsmsParameter = new HSMSParameter();
				var dbContext = scope.ServiceProvider.GetRequiredService<SecsGemDBContext>();
				return await dbContext.HSMSParameter.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1);
			}
		}

		private async Task SetHSMSParameter()
		{
			var hsmsParameter = new HSMSParameter();
			var hsms = await GetHSMSParameter();
			if (hsms is not null)
				hsmsParameter = hsms;

			qsWrapper.T3 = hsmsParameter.T3;
			qsWrapper.lDeviceID = hsmsParameter.DeviceID;
			qsWrapper.lCOMM_Mode = hsmsParameter.CommMode;

			qsWrapper.T5 = hsmsParameter.T5;
			qsWrapper.T6 = hsmsParameter.T6;
			qsWrapper.T7 = hsmsParameter.T7;
			qsWrapper.T8 = hsmsParameter.T8;
			qsWrapper.lLinkTestPeriod = hsmsParameter.LinkTestPeriod;
			qsWrapper.szLocalIP = hsmsParameter.LocalIP;
			qsWrapper.nLocalPort = hsmsParameter.LocalPort;
			qsWrapper.szRemoteIP = hsmsParameter.RemoteIP;
			qsWrapper.nRemotePort = hsmsParameter.RemotePort;
			qsWrapper.HSMS_Connect_Mode = hsmsParameter.HSMS_Connect_Mode;
			HSMSConfigChanged();
		}

		public async Task<RequestResult> InitAndStartHSMS()
		{
			await SetHSMSParameter();
			var initRes = qsWrapper.Initialize();
			if (initRes is not 0)
			{
				return new RequestResult(4, $"Init hsms fail({initRes})");
			}
			qsWrapper.QSEvent += new _IQSWrapperEvents_QSEventEventHandler(QSEvent);

			int hsmsPassiveRes = qsWrapper.Start();
			bool success = hsmsPassiveRes is 1;
			secsStatus.SetHosting(success);
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

		//public async Task<RequestResult> StopHSMS()
		//{
		//	var initRes = qsWrapper.Stop();
		//	if (initRes is not 0)
		//	{
		//		return new RequestResult(4, $"Stop hsms fail({initRes})");
		//	}
		//	else
		//	{
		//		return new RequestResult(4, $"Start hsms success");
		//	}
		//}

		private void QSEvent(int lID, EVENT_ID lMsgID, int S, int F, int W_Bit, int ulSystemBytes, object RawData, object Head, string pEventText)
		{
			var res = SecsParser.Parse(RawData);
			Log(lMsgID, S, F, res);
			var processRes = RunOnSTAThread<PROCESS_MSG_RESULT>(() => qgWrapper.ProcessMessage((int)lMsgID, S, F, W_Bit, ulSystemBytes, RawData, Head, pEventText));
			switch (lMsgID)
			{
				case EVENT_ID.QS_EVENT_CONNECTED:
					secsStatus.SetConnected(true);
					break;
				case EVENT_ID.QS_EVENT_RECV_MSG:
					break;
				case EVENT_ID.QS_EVENT_SEND_MSG:
					break;
				case EVENT_ID.QS_EVENT_DISCONNECTED:
					secsStatus.SetConnected(false);
					break;
				default:
					break;
			}
			UIUpdate();
		}


		public void GetSecsGemDefaultStatus()
		{
			UpdateCommunicatingDefaultStatus();
			UpdateDefaultControlState();
			UpdateDefaultOfflineSubstate();
			UpdateDefaultOnlineFailSubstate();
			UpdateDefaultOnlineSubstate();
		}

		public async Task UpdateGemStatus()
		{
			UpdateCommunicatingStatus();
			await UpdateControlStateStatus();
		}

		#region communicating
		private void UpdateCommunicatingDefaultStatus()
		{
			EC_DATA_TYPE lGetFormat;
			Object currentVal = -1;
			var updateRes = RunOnSTAThread(() => qgWrapper.GetEC(7, out lGetFormat, out currentVal));
			GemStatus.SetDefaultCommunicating(int.Parse(currentVal.ToString()) is 1);
		}

		public Task<RequestResult> SwitchDefaultCommunicatingStatus(bool b)
		{
			var res = RunOnSTAThread(() => qgWrapper.UpdateEC(7, b ? 1 : 0));
			UpdateCommunicatingDefaultStatus();
			return res is 0 ? Task.FromResult(new RequestResult(2, "Update default communicating status success")) : Task.FromResult(new RequestResult(4, $"Update default communicating status fail({res})"));
		}


		private void UpdateCommunicatingStatus()
		{
			var communicating = RunOnSTAThread(() => qgWrapper.GetCurrentCommState());
			GemStatus.SetCommunicating(communicating);
		}

		public void SwitchGemCommunicatingStatus(bool b)
		{
			if (b)
				RunOnSTAThread(() => qgWrapper.EnableComm());
			else
				RunOnSTAThread(() => qgWrapper.DisableComm());
			UpdateCommunicatingStatus();
		}



		#endregion



		#region control state

		private Task UpdateControlStateStatus()
		{
			SV_DATA_TYPE lGetFormat;
			Object currentVal = -1;
			var updateRes = RunOnSTAThread(() => qgWrapper.GetSV(4, out lGetFormat, out currentVal));
			int intRes = int.Parse(currentVal.ToString() ?? string.Empty);
			switch (intRes)
			{
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
					GemStatus.SetControlState((ControlState)intRes);
					break;
				default:
					break;
			}
			return Task.CompletedTask;
		}

		private void UpdateDefaultControlState()
		{
			EC_DATA_TYPE lGetFormat;
			Object currentVal = -1;
			var updateRes = RunOnSTAThread(() => qgWrapper.GetEC(8, out lGetFormat, out currentVal));
			GemStatus.SetDefaultControlState((DefaultControlState)int.Parse(currentVal.ToString()));
		}
		public Task<RequestResult> SwitchDefaultControlState(DefaultControlState defaultControlState)
		{
			object value = (int)defaultControlState;
			var res = RunOnSTAThread(() => qgWrapper.UpdateEC(8, value));
			UpdateDefaultControlState();
			return res is 0 ? Task.FromResult(new RequestResult(2, "Update default control state success")) : Task.FromResult(new RequestResult(4, $"Update default control status fail({res})"));

		}

		public Task<RequestResult> SendOnlineRequest()
		{
			var res = RunOnSTAThread(() => qgWrapper.OnLineRequest());
			return res is 0 ? Task.FromResult(new RequestResult(2, "Send online request success")) : Task.FromResult(new RequestResult(4, $"Send online request fail({res})"));
		}

		public Task<RequestResult> SendOffline()
		{
			var res = RunOnSTAThread(() => qgWrapper.OffLine());
			return res is 0 ? Task.FromResult(new RequestResult(2, "Send offline success")) : Task.FromResult(new RequestResult(4, $"Send offline fail({res})"));
		}

		public Task<RequestResult> SendOnLineRemote()
		{
			var res = RunOnSTAThread(() => qgWrapper.OnLineRemote());
			return res is 0 ? Task.FromResult(new RequestResult(2, "Send online remote success")) : Task.FromResult(new RequestResult(4, $"Send online remote fail({res})"));
		}

		public Task<RequestResult> SendOnLineLocal()
		{
			var res = RunOnSTAThread(() => qgWrapper.OnLineLocal());
			return res is 0 ? Task.FromResult(new RequestResult(2, "Send online local success")) : Task.FromResult(new RequestResult(4, $"Send online local fail({res})"));
		}

		private void UpdateDefaultOfflineSubstate()
		{
			EC_DATA_TYPE lGetFormat;
			Object currentVal = -1;
			var updateRes = RunOnSTAThread(() => qgWrapper.GetEC(49, out lGetFormat, out currentVal));
			GemStatus.SetDefaultOfflineSubstate((DefaultOfflineOrOnlineFailSubstate)int.Parse(currentVal.ToString()));
		}

		public Task<RequestResult> SwitchDefaultOfflineSubstate(DefaultOfflineOrOnlineFailSubstate defaultOfflineSubstate)
		{
			object value = (int)defaultOfflineSubstate;
			var res = RunOnSTAThread(() => qgWrapper.UpdateEC(49, value));
			UpdateDefaultOfflineSubstate();
			return res is 0 ? Task.FromResult(new RequestResult(2, "Update default offline substate success")) : Task.FromResult(new RequestResult(4, $"Update default offline substate fail({res})"));

		}

		private void UpdateDefaultOnlineFailSubstate()
		{
			EC_DATA_TYPE lGetFormat;
			Object currentVal = -1;
			var updateRes = RunOnSTAThread(() => qgWrapper.GetEC(50, out lGetFormat, out currentVal));
			GemStatus.SetDefaultOnlineFailSubstate((DefaultOfflineOrOnlineFailSubstate)int.Parse(currentVal.ToString()));
		}
		public Task<RequestResult> SwitchDefaultOnlineFailSubstate(DefaultOfflineOrOnlineFailSubstate defaultOfflineSubstate)
		{
			object value = (int)defaultOfflineSubstate;
			var res = RunOnSTAThread(() => qgWrapper.UpdateEC(50, value));
			UpdateDefaultOnlineFailSubstate();
			return res is 0 ? Task.FromResult(new RequestResult(2, "Update default online fail substate success")) : Task.FromResult(new RequestResult(4, $"Update default online fail substate fail({res})"));

		}

		private void UpdateDefaultOnlineSubstate()
		{
			EC_DATA_TYPE lGetFormat;
			Object currentVal = -1;
			var updateRes = RunOnSTAThread(() => qgWrapper.GetEC(51, out lGetFormat, out currentVal));
			GemStatus.SetDefaultOnlineSubstate((DefaultOnlineSubstate)int.Parse(currentVal.ToString()));
		}

		public Task<RequestResult> SwitchDefaultOnlineSubstate(DefaultOnlineSubstate defaultOnlineSubstate)
		{
			object value = (int)defaultOnlineSubstate;
			var res = RunOnSTAThread(() => qgWrapper.UpdateEC(51, value));
			UpdateDefaultOnlineSubstate();
			return res is 0 ? Task.FromResult(new RequestResult(2, "Update default online substate success")) : Task.FromResult(new RequestResult(4, $"Update default online substate fail({res})"));

		}

		#endregion

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
			secsStatus.SetHosting(!success);
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


		public RequestResult InitGem()
		{
			var qgRes = qgWrapper.Initialize(configPath);
			var res = qgRes is 0;
			if (res)
			{
				qgWrapper.QGEvent += new _IQGWrapperEvents_QGEventEventHandler(qgEvent);
				qgWrapper.PPEvent += new _IQGWrapperEvents_PPEventEventHandler(qgInfoEvent);
				qgWrapper.TerminalMsgReceive += new _IQGWrapperEvents_TerminalMsgReceiveEventHandler(qgTerminalMsgReceive);
				GetSecsGemDefaultStatus();
			}
			GemStatus.SetInitSuccess(res);
			return res ? new(2, "Init Gem success") : new(4, "Init Gem fail");
		}

		#region sv

		public async Task InitSVs()
		{
			using (var scope = scopeFactory.CreateScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<SecsGemDBContext>();
				SVs = await dbContext.SVs.AsNoTracking().ToListAsync();
			}
		}

		public Task UpdateSVs()
		{
			foreach (var sv in SVs)
			{
				Object? value;
				SV_DATA_TYPE _data_type;
				var res = qgWrapper.GetSV(sv.SVId, out _data_type, out value);
				if (res is 0)
				{
					sv.SetValue(value);
					sv.SV_DATA_TYPE = _data_type;
				}
			}
			SVUpdate();
			return Task.CompletedTask;
		}

		public Task SetSV(SetSVParameter setSVParameter)
		{
			var target = SVs.FirstOrDefault(sv => sv.Name == setSVParameter.Name);
			if (target is not null)
			{
				Object? obj = null;
				switch (target.SV_DATA_TYPE)
				{
					//case SV_DATA_TYPE.SV_ASCII_TYPE:
					//	obj = setSVParameter.ValueString;
					//	break;
					//case SV_DATA_TYPE.SV_BINARY_TYPE:
					//	if (byte.TryParse(setSVParameter.ValueString, out byte byteValue))
					//		obj = byteValue;
					//	break;
					//case SV_DATA_TYPE.SV_BOOLEAN_TYPE:
					//	if (bool.TryParse(setSVParameter.ValueString, out bool booleanValue))
					//		obj = booleanValue;
					//	break;
					//case SV_DATA_TYPE.SV_INT_1_TYPE:
					//	if (sbyte.TryParse(setSVParameter.ValueString, out sbyte sbyteValue))
					//		obj = sbyteValue;
					//	break;
					//case SV_DATA_TYPE.SV_INT_2_TYPE:
					//	if (short.TryParse(setSVParameter.ValueString, out short shortValue))
					//		obj = shortValue;
					//	break;
					//case SV_DATA_TYPE.SV_INT_4_TYPE:
					//	if (int.TryParse(setSVParameter.ValueString, out int intValue))
					//		obj = intValue;
					//	break;

					//case SV_DATA_TYPE.SV_UINT_1_TYPE:
					//	if (byte.TryParse(setSVParameter.ValueString, out byte u1ByteValue))
					//		obj = u1ByteValue;
					//	break;
					//case SV_DATA_TYPE.SV_UINT_2_TYPE:
					//	if (ushort.TryParse(setSVParameter.ValueString, out ushort ushortValue))
					//		obj = ushortValue;
					//	break;
					//case SV_DATA_TYPE.SV_UINT_4_TYPE:
					//	if (uint.TryParse(setSVParameter.ValueString, out uint uintValue))
					//		obj = uintValue;
					//	break;
					//case SV_DATA_TYPE.SV_FT_4_TYPE:
					//	if (float.TryParse(setSVParameter.ValueString, out float floatValue))
					//		obj = floatValue;
					//	break;
					//case SV_DATA_TYPE.SV_FT_8_TYPE:
					//	if (double.TryParse(setSVParameter.ValueString, out double doubleValue))
					//		obj = doubleValue;
					//	break;
					case SV_DATA_TYPE.SV_ASCII_TYPE:
						obj = setSVParameter.ValueString;
						break;

					case SV_DATA_TYPE.SV_BINARY_TYPE or SV_DATA_TYPE.SV_UINT_1_TYPE when byte.TryParse(setSVParameter.ValueString, out var u1):
						obj = u1;
						break;

					case SV_DATA_TYPE.SV_BOOLEAN_TYPE when bool.TryParse(setSVParameter.ValueString, out var b):
						obj = b;
						break;

					case SV_DATA_TYPE.SV_INT_1_TYPE when sbyte.TryParse(setSVParameter.ValueString, out var i1):
						obj = i1;
						break;

					case SV_DATA_TYPE.SV_INT_2_TYPE when short.TryParse(setSVParameter.ValueString, out var i2):
						obj = i2;
						break;

					case SV_DATA_TYPE.SV_INT_4_TYPE when int.TryParse(setSVParameter.ValueString, out var i4):
						obj = i4;
						break;

					case SV_DATA_TYPE.SV_UINT_2_TYPE when ushort.TryParse(setSVParameter.ValueString, out var u2):
						obj = u2;
						break;

					case SV_DATA_TYPE.SV_UINT_4_TYPE when uint.TryParse(setSVParameter.ValueString, out var u4):
						obj = u4;
						break;

					case SV_DATA_TYPE.SV_FT_4_TYPE when float.TryParse(setSVParameter.ValueString, out var f4):
						obj = f4;
						break;

					case SV_DATA_TYPE.SV_FT_8_TYPE when double.TryParse(setSVParameter.ValueString, out var f8):
						obj = f8;
						break;
					default:
						break;
				}
				if (obj is not null)
				{
					RunOnSTAThread(() => qgWrapper.UpdateSV(target.SVId, obj));
				}

			}
			else
			{

			}
			return Task.CompletedTask;
		}

		public async Task UpsertSV(SV sv)
		{
			await UpsertSVInDB(sv);
			UpsertSVInMemory(sv);
			SVUpdate();
		}
		private async Task<RequestResult> UpsertSVInDB(SV sv)
		{
			using (var scope = scopeFactory.CreateScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<SecsGemDBContext>();
				var target = await dbContext.SVs.FirstOrDefaultAsync(x => x.Id == sv.Id);
				if (target is not null)
				{
					dbContext.Entry(target).CurrentValues.SetValues(sv);
				}
				else
				{
					await dbContext.SVs.AddAsync(sv);
				}
				await dbContext.SaveChangesAsync();
				return new(2, $"Upsert sv config success)");
			}
		}
		private void UpsertSVInMemory(SV sv)
		{
			var target = SVs.FirstOrDefault(x => x.Id == sv.Id);
			if (target is not null)
			{
				target = sv;
			}
			else
			{
				SVs.Add(sv);
			}
		}

		public async Task DeleteSV(SV sv)
		{
			await DeleteSVInDB(sv);
			DeleteSVInMemory(sv);
			SVUpdate();
		}
		private async Task<RequestResult> DeleteSVInDB(SV sv)
		{
			using (var scope = scopeFactory.CreateScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<SecsGemDBContext>();
				var target = await dbContext.SVs.FirstOrDefaultAsync(x => x.Id == sv.Id);
				if (target is not null)
				{
					dbContext.SVs.Remove(target);
					await dbContext.SaveChangesAsync();
					return new(2, $"Delete sv config success)");
				}
				else
				{
					return new(4, $"Delete sv config fail)");
				}

			}
		}

		private void DeleteSVInMemory(SV sv)
		{
			var index = SVs.FindIndex(x => x.Id == sv.Id);
			if (index >= 0)
			{
				SVs.RemoveAt(index);
			}
		}

		#endregion

		#region event

		public Task<RequestResult> SendEvent(SendEventParameter sendEventParameter)
		{
			var res = RunOnSTAThread(() => qgWrapper.EventReportSend(sendEventParameter.EventId));
			return res is 0 ? Task.FromResult(new RequestResult(2, $"Send event {sendEventParameter.EventId} success")) : Task.FromResult(new RequestResult(4, $"Send event {sendEventParameter.EventId} fail({res})"))
;
		}

		#endregion

		#region remote command
		private RemoteCommand remoteCommand = new();

		private string parameterName = string.Empty;

		public event Func<RemoteCommand, Task>? RemoteCommandAction;

		private void ProcessRemoteCommandSuccess()
		{
			if (RemoteCommandAction != null)
			{
				foreach (Func<RemoteCommand, Task> handler in RemoteCommandAction.GetInvocationList())
				{
					_ = Task.Run(async () =>
					{
						try
						{
							await handler(remoteCommand);
						}
						catch (Exception ex)
						{
						}
					});
				}
			}
			//remoteCommand = new();
		}

		public int ReplyForRemoteCommand(Object obj)
		{
			Object obj2 = "";
			var res = RunOnSTAThread(() => qgWrapper.Command((int)QGACTIVEXLib.PP_TYPE.CMD_REPLY_S2F42_HCACK, ref obj, ref obj2));
			return res;
		}

		#endregion

		private void qgEvent(int lID, int S, int F, int W_Bit, int SystemBytes, object RawData, int Length)
		{
			//RunOnSTAThread(() => qsWrapper.SendSECSIIMessage(S, F, W_Bit, ref SystemBytes, RawData));

		}

		private void qgInfoEvent(PP_TYPE MsgID, string InfoData)
		{
			//parsing remote command
			if (MsgID is PP_TYPE.RECEIVE_S2F41_RCMD)
			{
				remoteCommand = new(InfoData);
			}
			else if (MsgID is PP_TYPE.RECEIVE_S2F41_CPNAME)
			{
				parameterName = InfoData;
			}
			else if (MsgID is PP_TYPE.RECEIVE_S2F41_CPVAL)
			{
				remoteCommand.ParameterList.Add(parameterName, InfoData);
				parameterName = string.Empty;
			}
			else if (MsgID is PP_TYPE.RECEIVE_S2F41_RCMD_END)
			{
				ProcessRemoteCommandSuccess();
			}
		}

		private void qgTerminalMsgReceive(string Message)
		{

		}


	}
}
