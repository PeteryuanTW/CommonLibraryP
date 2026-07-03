using CommonLibraryP.API;
using CommonLibraryP.Data;
using CommonLibraryP.ShopfloorPKG;
using NModbus;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public sealed class ModbusReadOptions
    {
        public ushort CoilBatchSize { get; set; } = 125;
        public ushort RegisterBatchSize { get; set; } = 125;
        public int MaxParallelBatches { get; set; } = 4;
        public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(2);
        public int MaxRetryCount { get; set; } = 2;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    }

    public class ModbusTCPMachine : Machine
    {
        private TcpClient _tcpClient;
        private readonly IModbusFactory _modbusFactory;
        public IModbusMaster? Master { get; private set; }

        // ---------------------------------------------------------
        // 手動設定（public 屬性，預設 null = AI 自適應）
        // ---------------------------------------------------------
        public int? ManualMaxParallelBatches { get; set; } = 2;
        public ushort? ManualRegisterBatchSize { get; set; } = 125;
        public int? ManualGapThreshold { get; set; } = 125;

        // ---------------------------------------------------------
        // AI 自適應參數
        // ---------------------------------------------------------
        private int _dynamicGapThreshold = 20;
        private readonly List<double> _responseHistory = new();

        private readonly ModbusReadOptions _readOptions = new()
        {
            CoilBatchSize = 125,
            RegisterBatchSize = 125,
            MaxParallelBatches = 4,
            BatchTimeout = TimeSpan.FromSeconds(2),
            MaxRetryCount = 2,
            RetryDelay = TimeSpan.FromMilliseconds(100)
        };

        // ---------------------------------------------------------
        // Stopwatch（AI 用來判斷 PLC 回應速度）
        // ---------------------------------------------------------
        private readonly Stopwatch _discreteInputStopwatch = new();
        public Stopwatch DiscreteInputStopwatch => _discreteInputStopwatch;

        private readonly Stopwatch _coilStopwatch = new();
        public Stopwatch CoilStopwatch => _coilStopwatch;

        private readonly Stopwatch _inputRegisterStopwatch = new();
        public Stopwatch InputRegisterStopwatch => _inputRegisterStopwatch;

        private readonly Stopwatch _holdingRegisterStopwatch = new();
        public Stopwatch HoldingRegisterStopwatch => _holdingRegisterStopwatch;

        // ---------------------------------------------------------
        // 無參數建構子（MachineService 反射需要）
        // ---------------------------------------------------------
        public ModbusTCPMachine() : base()
        {
            _tcpClient = new TcpClient();
            _modbusFactory = new ModbusFactory();
        }

        // ---------------------------------------------------------
        // ConnectAsync（整合 Dispose + Timeout）
        // ---------------------------------------------------------
        public sealed override async Task ConnectAsync()
        {
            try
            {
                retryCount++;
                TryConnecting();

                // 完整釋放舊連線
                _tcpClient?.Dispose();
                _tcpClient = new TcpClient();

                // 加入 Timeout（避免卡死）
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

                await _tcpClient.ConnectAsync(Ip, Port, cts.Token);

                Master = _modbusFactory.CreateMaster(_tcpClient);

                FetchingData();
                retryCount = 0;
            }
            catch (OperationCanceledException)
            {
                Disconnect("Connect timeout");
            }
            catch (SocketException e)
            {
                Disconnect(e.Message);
            }
            catch (Exception e)
            {
                Error(e.Message);
            }
        }

        // ---------------------------------------------------------
        // AutoTune（AI 自適應 + 手動優先權）
        // ---------------------------------------------------------
        private void AutoTune(List<ModbusTCPTag> tags, int segmentCount)
        {
            int gap = ManualGapThreshold ?? AutoGapThreshold(tags);
            gap = AdjustGapBySegmentCount(gap, segmentCount);

            double avgMs =
                (DiscreteInputStopwatch.ElapsedMilliseconds +
                 CoilStopwatch.ElapsedMilliseconds +
                 InputRegisterStopwatch.ElapsedMilliseconds +
                 HoldingRegisterStopwatch.ElapsedMilliseconds) / 4.0;

            if (avgMs > 0)
                _responseHistory.Add(avgMs);

            double learnedAvg = _responseHistory.Count > 5
                ? _responseHistory.Skip(Math.Max(0, _responseHistory.Count - 5)).Average()
                : avgMs;

            _readOptions.MaxParallelBatches =
                ManualMaxParallelBatches ?? AutoParallel(learnedAvg);

            ushort batch = ManualRegisterBatchSize ?? AutoBatchSize(learnedAvg);
            _readOptions.RegisterBatchSize = batch;
            _readOptions.CoilBatchSize = batch;

            _dynamicGapThreshold = gap;
        }

        private int AutoGapThreshold(List<ModbusTCPTag> tags)
        {
            if (tags.Count < 2)
                return 20;

            var sorted = tags.OrderBy(t => t.StartIndex).ToList();
            var gaps = new List<int>();

            for (int i = 1; i < sorted.Count; i++)
            {
                var prevEnd = sorted[i - 1].StartIndex + sorted[i - 1].Offset;
                var gap = sorted[i].StartIndex - prevEnd;
                if (gap > 0)
                    gaps.Add(gap);
            }

            if (gaps.Count == 0)
                return 30;

            var avg = gaps.Average();

            if (avg < 5) return 40;
            if (avg < 20) return 25;
            if (avg < 50) return 10;
            return 5;
        }

        private int AutoParallel(double avgMs)
        {
            if (avgMs < 10) return 4;
            if (avgMs < 20) return 3;
            if (avgMs < 40) return 2;
            return 1;
        }

        private ushort AutoBatchSize(double avgMs)
        {
            if (avgMs < 10) return 120;
            if (avgMs < 20) return 80;
            if (avgMs < 40) return 60;
            return 40;
        }

        private int AdjustGapBySegmentCount(int gap, int segmentCount)
        {
            if (segmentCount > 20) return gap + 10;
            if (segmentCount < 3) return Math.Max(5, gap - 5);
            return gap;
        }

        private List<(ushort start, ushort amount, List<ModbusTCPTag> tags)>
            BuildSmartSegments(List<ModbusTCPTag> tags, int gapThreshold)
        {
            if (tags == null || tags.Count == 0)
                return new();

            var sorted = tags.OrderBy(t => t.StartIndex).ToList();
            var segments = new List<List<ModbusTCPTag>>();
            var current = new List<ModbusTCPTag> { sorted[0] };

            for (int i = 1; i < sorted.Count; i++)
            {
                var prev = sorted[i - 1];
                var next = sorted[i];

                var prevEnd = prev.StartIndex + prev.Offset;
                var gap = next.StartIndex - prevEnd;

                if (gap > gapThreshold)
                {
                    segments.Add(current);
                    current = new List<ModbusTCPTag>();
                }

                current.Add(next);
            }

            segments.Add(current);

            var result = new List<(ushort start, ushort amount, List<ModbusTCPTag> tags)>();

            foreach (var seg in segments)
            {
                var start = (ushort)seg.Min(t => t.StartIndex);
                var end = (ushort)seg.Max(t => t.StartIndex + t.Offset);
                var amount = (ushort)(end - start);

                result.Add((start, amount, seg));
            }

            return result;
        }
        // ---------------------------------------------------------
        // UpdateTags（整合 AI 自適應 + 手動優先權 + 智慧分段）
        // ---------------------------------------------------------
        protected sealed override async Task UpdateTags()
        {
            if (!HasCategory || TagCategory?.Tags == null)
                return;

            var modbusTagsByStation = TagCategory.Tags
                .OfType<ModbusTCPTag>()
                .GroupBy(x => x.Station);

            foreach (var tagsByStation in modbusTagsByStation)
            {
                var updateGroups = tagsByStation
                    .Where(x => x.UpdateByTime)
                    .GroupBy(x => x.Station)
                    .Select(x => new { Station = x.Key, Tags = x.ToList() })
                    .ToList();

                foreach (var stationAndTags in updateGroups)
                {
                    // 先用舊參數做一次分段
                    var tempSegments = BuildSmartSegments(
                        stationAndTags.Tags,
                        ManualGapThreshold ?? _dynamicGapThreshold
                    );

                    // AI 自動調整（手動優先）
                    AutoTune(stationAndTags.Tags, tempSegments.Count);

                    // 用 AI + 手動後的 gapThreshold 重建分段
                    var segments = BuildSmartSegments(
                        stationAndTags.Tags,
                        ManualGapThreshold ?? _dynamicGapThreshold
                    );

                    // 更新四大區塊
                    await UpdateDiscreteInputsAsync(stationAndTags.Station, stationAndTags.Tags);
                    await UpdateCoilsAsync(stationAndTags.Station, stationAndTags.Tags);
                    await UpdateInputRegistersAsync(stationAndTags.Station, stationAndTags.Tags);
                    await UpdateHoldingRegistersAsync(stationAndTags.Station, stationAndTags.Tags);
                }
            }

            await base.UpdateTags();
        }

        // ---------------------------------------------------------
        // Boolean Input (DI)
        // ---------------------------------------------------------
        private async Task UpdateDiscreteInputsAsync(byte station, List<ModbusTCPTag> tags)
        {
            var boolInputTags = tags.Where(x => x.IsBoolean && !x.InputOrOutput).ToList();
            if (boolInputTags.Count == 0 || Master == null)
                return;

            _discreteInputStopwatch.Restart();

            var segments = BuildSmartSegments(boolInputTags, ManualGapThreshold ?? _dynamicGapThreshold);

            foreach (var seg in segments)
            {
                var data = await ReadInParallelAsync(
                    (addr, size, ct) => SafeReadBoolBatchAsync(
                        (s, a, c) => Master.ReadInputsAsync(s, a, c),
                        station,
                        addr,
                        size,
                        ct,
                        _readOptions
                    ),
                    seg.start,
                    seg.amount,
                    _readOptions.CoilBatchSize,
                    _readOptions,
                    CancellationToken.None
                );

                foreach (var tag in seg.tags)
                {
                    var offset = tag.StartIndex - seg.start;
                    var len = tag.Offset;

                    if (!tag.IsMultipleValue)
                    {
                        if (offset >= 0 && offset < data.Length)
                            tag.SetValue(data[offset]);
                    }
                    else
                    {
                        if (offset >= 0 && offset + len <= data.Length)
                            tag.SetValue(data.AsSpan(offset, len).ToArray());
                    }
                }
            }

            _discreteInputStopwatch.Stop();
        }

        // ---------------------------------------------------------
        // Boolean Output (Coils)
        // ---------------------------------------------------------
        private async Task UpdateCoilsAsync(byte station, List<ModbusTCPTag> tags)
        {
            var boolOutputTags = tags.Where(x => x.IsBoolean && x.InputOrOutput).ToList();
            if (boolOutputTags.Count == 0 || Master == null)
                return;

            _coilStopwatch.Restart();

            var segments = BuildSmartSegments(boolOutputTags, ManualGapThreshold ?? _dynamicGapThreshold);

            foreach (var seg in segments)
            {
                var data = await ReadInParallelAsync(
                    (addr, size, ct) => SafeReadBoolBatchAsync(
                        (s, a, c) => Master.ReadCoilsAsync(s, a, c),
                        station,
                        addr,
                        size,
                        ct,
                        _readOptions
                    ),
                    seg.start,
                    seg.amount,
                    _readOptions.CoilBatchSize,
                    _readOptions,
                    CancellationToken.None
                );

                foreach (var tag in seg.tags)
                {
                    var offset = tag.StartIndex - seg.start;
                    var len = tag.Offset;

                    if (!tag.IsMultipleValue)
                    {
                        if (offset >= 0 && offset < data.Length)
                            tag.SetValue(data[offset]);
                    }
                    else
                    {
                        if (offset >= 0 && offset + len <= data.Length)
                            tag.SetValue(data.AsSpan(offset, len).ToArray());
                    }
                }
            }

            _coilStopwatch.Stop();
        }

        // ---------------------------------------------------------
        // Input Registers (IR)
        // ---------------------------------------------------------
        private async Task UpdateInputRegistersAsync(byte station, List<ModbusTCPTag> tags)
        {
            var regTags = tags.Where(x => (x.IsUshort || x.IsString) && !x.InputOrOutput).ToList();
            if (regTags.Count == 0 || Master == null)
                return;

            _inputRegisterStopwatch.Restart();

            var segments = BuildSmartSegments(regTags, ManualGapThreshold ?? _dynamicGapThreshold);

            foreach (var seg in segments)
            {
                var data = await ReadInParallelAsync(
                    (addr, size, ct) => SafeReadUshortBatchAsync(
                        (s, a, c) => Master.ReadInputRegistersAsync(s, a, c),
                        station,
                        addr,
                        size,
                        ct,
                        _readOptions
                    ),
                    seg.start,
                    seg.amount,
                    _readOptions.RegisterBatchSize,
                    _readOptions,
                    CancellationToken.None
                );

                foreach (var tag in seg.tags)
                {
                    var offset = tag.StartIndex - seg.start;
                    var len = tag.Offset;

                    if (!tag.IsMultipleValue)
                    {
                        if (!tag.IsString)
                        {
                            if (offset >= 0 && offset < data.Length)
                                tag.SetValue(data[offset]);
                        }
                        else
                        {
                            if (offset >= 0 && offset + len <= data.Length)
                            {
                                var slice = data.AsSpan(offset, len).ToArray();
                                var str = UshortToString(slice, tag.StringReverse);
                                tag.SetValue(str);
                            }
                        }
                    }
                    else
                    {
                        if (offset >= 0 && offset + len <= data.Length)
                            tag.SetValue(data.AsSpan(offset, len).ToArray());
                    }
                }
            }

            _inputRegisterStopwatch.Stop();
        }

        // ---------------------------------------------------------
        // Holding Registers (HR)
        // ---------------------------------------------------------
        private async Task UpdateHoldingRegistersAsync(byte station, List<ModbusTCPTag> tags)
        {
            var regTags = tags.Where(x => (x.IsUshort || x.IsString) && x.InputOrOutput).ToList();
            if (regTags.Count == 0 || Master == null)
                return;

            _holdingRegisterStopwatch.Restart();

            var segments = BuildSmartSegments(regTags, ManualGapThreshold ?? _dynamicGapThreshold);

            foreach (var seg in segments)
            {
                var data = await ReadInParallelAsync(
                    (addr, size, ct) => SafeReadUshortBatchAsync(
                        (s, a, c) => Master.ReadHoldingRegistersAsync(s, a, c),
                        station,
                        addr,
                        size,
                        ct,
                        _readOptions
                    ),
                    seg.start,
                    seg.amount,
                    _readOptions.RegisterBatchSize,
                    _readOptions,
                    CancellationToken.None
                );

                foreach (var tag in seg.tags)
                {
                    var offset = tag.StartIndex - seg.start;
                    var len = tag.Offset;

                    if (!tag.IsMultipleValue)
                    {
                        if (!tag.IsString)
                        {
                            if (offset >= 0 && offset < data.Length)
                                tag.SetValue(data[offset]);
                        }
                        else
                        {
                            if (offset >= 0 && offset + len <= data.Length)
                            {
                                var slice = data.AsSpan(offset, len).ToArray();
                                var str = UshortToString(slice, tag.StringReverse);
                                tag.SetValue(str);
                            }
                        }
                    }
                    else
                    {
                        if (offset >= 0 && offset + len <= data.Length)
                            tag.SetValue(data.AsSpan(offset, len).ToArray());
                    }
                }
            }

            _holdingRegisterStopwatch.Stop();
        }

        // ---------------------------------------------------------
        // ReadInParallelAsync（平行讀取 + Timeout + Retry）
        // ---------------------------------------------------------
        private static async Task<T[]> ReadInParallelAsync<T>(
            Func<ushort, ushort, CancellationToken, Task<T[]>> readFunc,
            ushort startIndex,
            ushort totalAmount,
            ushort batchSize,
            ModbusReadOptions options,
            CancellationToken cancellationToken)
        {
            if (totalAmount == 0)
                return Array.Empty<T>();

            var result = new T[totalAmount];
            ushort remaining = totalAmount;
            ushort currentOffset = 0;

            while (remaining > 0)
            {
                var tasks = new List<Task<T[]>>();
                var offsets = new List<ushort>();

                for (int i = 0; i < options.MaxParallelBatches && remaining > 0; i++)
                {
                    var size = (ushort)Math.Min(remaining, batchSize);
                    var addr = (ushort)(startIndex + currentOffset);
                    var localOffset = currentOffset;

                    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(options.BatchTimeout);

                    var task = readFunc(addr, size, cts.Token);
                    tasks.Add(task);
                    offsets.Add(localOffset);

                    remaining -= size;
                    currentOffset += size;
                }

                var batchResults = await Task.WhenAll(tasks);

                for (int i = 0; i < batchResults.Length; i++)
                {
                    var partial = batchResults[i];
                    if (partial != null)
                        Array.Copy(partial, 0, result, offsets[i], partial.Length);
                }
            }

            return result;
        }
        // ---------------------------------------------------------
        // SafeReadBoolBatchAsync（含 Timeout + Retry）
        // ---------------------------------------------------------
        private static async Task<bool[]> SafeReadBoolBatchAsync(
            Func<byte, ushort, ushort, Task<bool[]>> rawRead,
            byte station,
            ushort address,
            ushort size,
            CancellationToken cancellationToken,
            ModbusReadOptions options)
        {
            int attempt = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var readTask = rawRead(station, address, size);
                    var completed = await Task.WhenAny(readTask, Task.Delay(options.BatchTimeout, cancellationToken));

                    if (completed != readTask)
                        throw new TimeoutException($"Modbus read timeout at address {address}, size {size}");

                    var data = await readTask;
                    if (data == null || data.Length != size)
                        throw new IOException($"Modbus read size mismatch at address {address}, expected {size}, got {data?.Length ?? 0}");

                    return data;
                }
                catch when (attempt < options.MaxRetryCount)
                {
                    attempt++;
                    await Task.Delay(options.RetryDelay, cancellationToken);
                }
            }
        }

        // ---------------------------------------------------------
        // SafeReadUshortBatchAsync（含 Timeout + Retry）
        // ---------------------------------------------------------
        private static async Task<ushort[]> SafeReadUshortBatchAsync(
            Func<byte, ushort, ushort, Task<ushort[]>> rawRead,
            byte station,
            ushort address,
            ushort size,
            CancellationToken cancellationToken,
            ModbusReadOptions options)
        {
            int attempt = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var readTask = rawRead(station, address, size);
                    var completed = await Task.WhenAny(readTask, Task.Delay(options.BatchTimeout, cancellationToken));

                    if (completed != readTask)
                        throw new TimeoutException($"Modbus read timeout at address {address}, size {size}");

                    var data = await readTask;
                    if (data == null || data.Length != size)
                        throw new IOException($"Modbus read size mismatch at address {address}, expected {size}, got {data?.Length ?? 0}");

                    return data;
                }
                catch when (attempt < options.MaxRetryCount)
                {
                    attempt++;
                    await Task.Delay(options.RetryDelay, cancellationToken);
                }
            }
        }

        // ---------------------------------------------------------
        // UpdateTag（單點讀取）
        // ---------------------------------------------------------
        public sealed override async Task<RequestResult> UpdateTag(Tag tag)
        {
            try
            {
                if (tag is not ModbusTCPTag mTag)
                    return new(1, "Tag type error");

                if (!RunFlag)
                    return new(1, $"Machine status {CommonEnumHelper.GetStatusDetail(StatusCode)} is not allow to update tag");

                if (Master == null)
                    return new(4, "Modbus master is null");

                var station = mTag.Station;
                var startIndex = mTag.StartIndex;
                var offset = mTag.Offset;
                var output = mTag.InputOrOutput;

                switch (tag.DataType)
                {
                    case 1: // bool
                        {
                            bool[] data = output
                                ? await Master.ReadCoilsAsync(station, startIndex, offset)
                                : await Master.ReadInputsAsync(station, startIndex, offset);

                            bool value = data.Length > 0 ? data[0] : false;
                            return tag.SetValue(value);
                        }

                    case 2: // ushort
                        {
                            ushort[] data = output
                                ? await Master.ReadHoldingRegistersAsync(station, startIndex, offset)
                                : await Master.ReadInputRegistersAsync(station, startIndex, offset);

                            ushort value = data.Length > 0 ? data[0] : (ushort)0;
                            return tag.SetValue(value);
                        }

                    case 4: // string
                        {
                            ushort[] data = output
                                ? await Master.ReadHoldingRegistersAsync(station, startIndex, offset)
                                : await Master.ReadInputRegistersAsync(station, startIndex, offset);

                            var str = UshortToString(data, mTag.StringReverse);
                            return tag.SetValue(str);
                        }

                    case 11: // bool[]
                        {
                            bool[] data = output
                                ? await Master.ReadCoilsAsync(station, startIndex, offset)
                                : await Master.ReadInputsAsync(station, startIndex, offset);

                            return tag.SetValue(data);
                        }

                    case 22: // ushort[]
                        {
                            ushort[] data = output
                                ? await Master.ReadHoldingRegistersAsync(station, startIndex, offset)
                                : await Master.ReadInputRegistersAsync(station, startIndex, offset);

                            return tag.SetValue(data);
                        }

                    default:
                        return new(4, "Not implement yet");
                }
            }
            catch (Exception e)
            {
                Error(e.Message);
                return new(4, $"Update tag fail({e.Message})");
            }
        }

        // ---------------------------------------------------------
        // SetTag（單點寫入）
        // ---------------------------------------------------------
        public sealed override async Task<RequestResult> SetTag(Tag tag, object val)
        {
            if (tag is not ModbusTCPTag mTag)
                return new(4, "Tag type error");

            if (Master == null)
                return new(4, "Modbus master is null");

            if (!mTag.InputOrOutput)
                return new(4, "Input is not allow to set");

            var station = mTag.Station;
            var startIndex = mTag.StartIndex;
            var offset = mTag.Offset;
            var stringReverse = mTag.StringReverse;

            try
            {
                switch (tag.DataType)
                {
                    case 1: // bool
                        if (val is not bool boolVal)
                            return new(4, "Data is not boolean type");

                        await Master.WriteSingleCoilAsync(station, startIndex, boolVal);
                        var boolRead = await Master.ReadCoilsAsync(station, startIndex, offset);
                        return tag.SetValue(boolRead.Length > 0 ? boolRead[0] : false);

                    case 2: // ushort
                        if (val is not ushort ushortVal)
                            return new(4, "Data is not ushort type");

                        await Master.WriteSingleRegisterAsync(station, startIndex, ushortVal);
                        var ushortRead = await Master.ReadHoldingRegistersAsync(station, startIndex, offset);
                        return tag.SetValue(ushortRead.Length > 0 ? ushortRead[0] : (ushort)0);

                    case 4: // string
                        if (val is not string strVal)
                            return new(4, "Data is not string type");

                        var reset = Enumerable.Repeat((ushort)0, offset).ToArray();
                        await Master.WriteMultipleRegistersAsync(station, startIndex, reset);

                        if (!string.IsNullOrEmpty(strVal))
                        {
                            var data = StringToUshortArray(strVal, stringReverse, offset);
                            await Master.WriteMultipleRegistersAsync(station, startIndex, data);
                        }

                        var readback = await Master.ReadHoldingRegistersAsync(station, startIndex, offset);
                        var readStr = UshortToString(readback, stringReverse);
                        return tag.SetValue(readStr);

                    case 11: // bool[]
                        if (val is not bool[] boolArr)
                            return new(4, "Data is not boolean array type");

                        if (boolArr.Length != offset)
                            return new(4, $"Boolean array length {boolArr.Length} and offset {offset} not match");

                        await Master.WriteMultipleCoilsAsync(station, startIndex, boolArr);
                        var boolArrRead = await Master.ReadCoilsAsync(station, startIndex, offset);
                        return tag.SetValue(boolArrRead);

                    case 22: // ushort[]
                        if (val is not ushort[] ushortArr)
                            return new(4, "Data is not ushort array type");

                        if (ushortArr.Length != offset)
                            return new(4, $"Ushort array length {ushortArr.Length} and offset {offset} not match");

                        await Master.WriteMultipleRegistersAsync(station, startIndex, ushortArr);
                        var ushortArrRead = await Master.ReadHoldingRegistersAsync(station, startIndex, offset);
                        return tag.SetValue(ushortArrRead);

                    default:
                        return new(3, "Not implement yet");
                }
            }
            catch (Exception e)
            {
                Error(e.Message);
                return new(4, $"Set tag fail({e.Message})");
            }
        }

        // ---------------------------------------------------------
        // StringToUshortArray（最佳化，不浪費記憶體）
        // ---------------------------------------------------------
        private static ushort[] StringToUshortArray(string s, bool reverse, int maxRegisters)
        {
            if (string.IsNullOrEmpty(s))
                return Enumerable.Repeat((ushort)0, maxRegisters).ToArray();

            byte[] bytes = Encoding.ASCII.GetBytes(s);

            if (bytes.Length % 2 != 0)
                bytes = bytes.Concat(new byte[] { 0x00 }).ToArray();

            ushort[] result = new ushort[maxRegisters];
            int count = Math.Min(maxRegisters * 2, bytes.Length);

            for (int i = 0, r = 0; i < count && r < maxRegisters; i += 2, r++)
            {
                if (reverse)
                    result[r] = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(i, 2));
                else
                    result[r] = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(i, 2));
            }

            return result;
        }

        // ---------------------------------------------------------
        // UshortToString（全新模式 B：找到最後一個非 0 byte）
        // ---------------------------------------------------------
        private static string UshortToString(ushort[] ushorts, bool reverse)
        {
            if (ushorts == null || ushorts.Length == 0)
                return string.Empty;

            byte[] buffer = new byte[ushorts.Length * 2];
            int index = 0;

            foreach (var u in ushorts)
            {
                byte[] b = BitConverter.GetBytes(u);

                if (reverse)
                    Array.Reverse(b);

                buffer[index++] = b[0];
                buffer[index++] = b[1];
            }

            // 找到最後一個非 0 byte（避免中間 0x00 截斷）
            int lastNonZero = -1;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] != 0)
                    lastNonZero = i;
            }

            if (lastNonZero < 0)
                return string.Empty;

            return Encoding.ASCII.GetString(buffer, 0, lastNonZero + 1);
        }
    }
}
