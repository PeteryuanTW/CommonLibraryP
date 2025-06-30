using CommonLibraryP.API;
using CommonLibraryP.Data;
using CommonLibraryP.ShopfloorPKG;
using DevExpress.Blazor.Internal;
using DevExpress.Pdf.ContentGeneration.Interop;
using NModbus;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public class ModbusTCPMachine : Machine
    {
        private TcpClient tcpClient;
        private IModbusFactory modbusFactory;
        public IModbusMaster? master;


        
        private ushort coilBatch = 100;
        private ushort registerBatch = 100;

        private Stopwatch inputCoilStopwatch = new();
        public Stopwatch InputCoilStopwatch => inputCoilStopwatch;

        private Stopwatch outputCoilStopwatch = new();
        public Stopwatch OutputCoilStopwatch => outputCoilStopwatch;

        private Stopwatch inputRegisterStopwatch = new();
        public Stopwatch InputRegisterStopwatch => inputRegisterStopwatch;

        private Stopwatch outputRegisterStopwatch = new();
        public Stopwatch OutputRegisterStopwatch => outputRegisterStopwatch;


        public ModbusTCPMachine() : base()
        {
            tcpClient = new();
            modbusFactory = new ModbusFactory();
        }

        public sealed override async Task ConnectAsync()
        {
            try
            {
                retryCount++;
                TryConnecting();
                tcpClient.Close();
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(Ip, Port);
                master = modbusFactory.CreateMaster(tcpClient);
                FetchingData();
                retryCount = 0;
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

        protected sealed override async Task UpdateTags()
        {
            if (!hasCategory)
            {
                return;
            }
            var modbusTagsByStation = TagCategory?.Tags.OfType<ModbusTCPTag>().GroupBy(x => x.Station);
            foreach (var tagsByStation in modbusTagsByStation)
            {
                var updateBytimeTags = tagsByStation.Where(x => x.UpdateByTime).GroupBy(x => x.Station)
                    .Select(x => new { station = x.Key, tags = x.ToList() }).ToList();
                foreach (var stationAndTags in updateBytimeTags)
                {
                    #region input coils
                    var boolInputTags = stationAndTags.tags.Where(x => x.IsBoolean && !x.InputOrOutput).ToList();
                    if (boolInputTags is not null && boolInputTags.Count > 0)
                    {
                        inputCoilStopwatch.Restart();
                        var boolInputStart = boolInputTags.Select(x => x.StartIndex).Min();
                        var boolInputEnd = boolInputTags.Select(x => x.StartIndex + x.Offset).Max();
                        var boolInputAmount = (ushort)(boolInputEnd - boolInputStart);

                        bool[] boolInputResult = new bool[boolInputAmount];
                        ushort boolInputRemaining = boolInputAmount;
                        ushort boolInputOffset = 0;

                        while (boolInputRemaining > 0)
                        {
                            ushort batchSize = Math.Min(boolInputRemaining, coilBatch);
                            bool[] partial = await master?.ReadInputsAsync(stationAndTags.station, (ushort)(boolInputStart + boolInputOffset), (ushort)batchSize);

                            if (partial == null || partial.Length != batchSize)
                            {

                            }

                            Array.Copy(partial, 0, boolInputResult, boolInputOffset, batchSize);

                            boolInputRemaining -= batchSize;
                            boolInputOffset += batchSize;
                        }
                        //set tags from data
                        foreach (var boolInputTag in boolInputTags)
                        {
                            if (!boolInputTag.IsMultipleValue)
                            {
                                boolInputTag.SetValue(boolInputResult[boolInputTag.StartIndex - boolInputStart]);
                            }
                            else
                            {
                                boolInputTag.SetValue(boolInputResult[(boolInputTag.StartIndex - boolInputStart)..(boolInputTag.StartIndex - boolInputStart + boolInputTag.Offset)]);
                            }
                        }
                        inputCoilStopwatch.Stop();
                    }
                    #endregion

                    #region output coils
                    var boolOutputTags = stationAndTags.tags.Where(x => x.IsBoolean && x.InputOrOutput).ToList();
                    if (boolOutputTags is not null && boolOutputTags.Count > 0)
                    {
                        outputCoilStopwatch.Restart();
                        var boolOutputStart = boolOutputTags.Select(x => x.StartIndex).Min();
                        var boolOutputEnd = boolOutputTags.Select(x => x.StartIndex + x.Offset).Max();
                        var boolOutputAmount = (ushort)(boolOutputEnd - boolOutputStart);

                        bool[] boolOutputResult = new bool[boolOutputAmount];
                        ushort boolOutputRemaining = boolOutputAmount;
                        ushort boolOutputOffset = 0;

                        while (boolOutputRemaining > 0)
                        {
                            ushort batchSize = Math.Min(boolOutputRemaining, coilBatch);
                            bool[] partial = await master?.ReadInputsAsync(stationAndTags.station, (ushort)(boolOutputStart + boolOutputOffset), (ushort)batchSize);

                            if (partial == null || partial.Length != batchSize)
                            {

                            }

                            Array.Copy(partial, 0, boolOutputResult, boolOutputOffset, batchSize);

                            boolOutputRemaining -= batchSize;
                            boolOutputOffset += batchSize;
                        }


                        foreach (var boolOutputTag in boolOutputTags)
                        {
                            if (!boolOutputTag.IsMultipleValue)
                            {
                                boolOutputTag.SetValue(boolOutputResult[boolOutputTag.StartIndex - boolOutputStart]);
                            }
                            else
                            {
                                boolOutputTag.SetValue(boolOutputResult[(boolOutputTag.StartIndex - boolOutputStart)..(boolOutputTag.StartIndex - boolOutputStart + boolOutputTag.Offset)]);
                            }
                        }
                        outputCoilStopwatch.Stop();
                    }
                    #endregion

                    #region input registers/strings
                    var ushortOrStringInputTags = stationAndTags.tags.Where(x => (x.IsUshort || x.IsString) && !x.InputOrOutput).ToList();
                    if (ushortOrStringInputTags is not null && ushortOrStringInputTags.Count > 0)
                    {
                        inputRegisterStopwatch.Restart();
                        var ushortOrStringInputStart = ushortOrStringInputTags.Select(x => x.StartIndex).Min();
                        var ushortOrStringInputEnd = ushortOrStringInputTags.Select(x => x.StartIndex + x.Offset).Max();
                        var ushortOrStringInputAmount = (ushort)(ushortOrStringInputEnd - ushortOrStringInputStart);

                        ushort[] ushortOrStringInputResult = new ushort[ushortOrStringInputAmount];
                        ushort ushortOrStringInputRemaining = ushortOrStringInputAmount;
                        ushort ushortOrStringInputOffset = 0;


                        var stopwatch = Stopwatch.StartNew();
                        while (ushortOrStringInputRemaining > 0)
                        {
                            ushort batchSize = Math.Min(ushortOrStringInputRemaining, registerBatch);
                            ushort[] partial = await master?.ReadInputRegistersAsync(stationAndTags.station, (ushort)(ushortOrStringInputStart + ushortOrStringInputOffset), (ushort)batchSize);

                            if (partial == null || partial.Length != batchSize)
                            {

                            }

                            Array.Copy(partial, 0, ushortOrStringInputResult, ushortOrStringInputOffset, batchSize);

                            ushortOrStringInputRemaining -= batchSize;
                            ushortOrStringInputOffset += batchSize;
                        }


                        foreach (var ushortOrStringInputTag in ushortOrStringInputTags)
                        {
                            if (!ushortOrStringInputTag.IsMultipleValue)
                            {
                                if (!ushortOrStringInputTag.IsString)
                                {
                                    //ushort
                                    ushortOrStringInputTag.SetValue(ushortOrStringInputResult[ushortOrStringInputTag.StartIndex - ushortOrStringInputStart]);
                                }
                                else
                                {
                                    //string
                                    var str = UshortToString(ushortOrStringInputResult[(ushortOrStringInputTag.StartIndex - ushortOrStringInputStart)..(ushortOrStringInputTag.StartIndex - ushortOrStringInputStart + ushortOrStringInputTag.Offset)], ushortOrStringInputTag.StringReverse);
                                    ushortOrStringInputTag.SetValue(str);
                                }
                            }
                            else
                            {
                                ushortOrStringInputTag.SetValue(ushortOrStringInputResult[(ushortOrStringInputTag.StartIndex - ushortOrStringInputStart)..(ushortOrStringInputTag.StartIndex - ushortOrStringInputStart + ushortOrStringInputTag.Offset)]);
                            }
                        }
                        inputRegisterStopwatch.Stop();
                    }
                    #endregion

                    #region output registers/strings
                    var ushortOrStringOutputTags = stationAndTags.tags.Where(x => (x.IsUshort || x.IsString) && x.InputOrOutput).ToList();
                    if (ushortOrStringOutputTags is not null && ushortOrStringOutputTags.Count > 0)
                    {
                        outputRegisterStopwatch.Restart();
                        var ushortOrStringOutputStart = ushortOrStringOutputTags.Select(x => x.StartIndex).Min();
                        var ushortOrStringOutputEnd = ushortOrStringOutputTags.Select(x => x.StartIndex + x.Offset).Max();
                        var ushortOrStringOutputAmount = (ushort)(ushortOrStringOutputEnd - ushortOrStringOutputStart);

                        ushort[] ushortOrStringOutputResult = new ushort[ushortOrStringOutputAmount];
                        ushort ushortOrStringOutputRemaining = ushortOrStringOutputAmount;
                        ushort ushortOrStringOutputOffset = 0;


                        //var stopwatch = Stopwatch.StartNew();
                        while (ushortOrStringOutputRemaining > 0)
                        {
                            ushort batchSize = Math.Min(ushortOrStringOutputRemaining, registerBatch);
                            ushort[] partial = await master?.ReadInputRegistersAsync(stationAndTags.station, (ushort)(ushortOrStringOutputStart + ushortOrStringOutputOffset), (ushort)batchSize);

                            if (partial == null || partial.Length != batchSize)
                            {

                            }

                            Array.Copy(partial, 0, ushortOrStringOutputResult, ushortOrStringOutputOffset, batchSize);

                            ushortOrStringOutputRemaining -= batchSize;
                            ushortOrStringOutputOffset += batchSize;
                        }


                        foreach (var ushortOrStringOutputTag in ushortOrStringOutputTags)
                        {
                            if (!ushortOrStringOutputTag.IsMultipleValue)
                            {
                                if (!ushortOrStringOutputTag.IsString)
                                {
                                    //ushort
                                    ushortOrStringOutputTag.SetValue(ushortOrStringOutputResult[ushortOrStringOutputTag.StartIndex - ushortOrStringOutputStart]);
                                }
                                else
                                {
                                    //string
                                    var str = UshortToString(ushortOrStringOutputResult[(ushortOrStringOutputTag.StartIndex - ushortOrStringOutputStart)..(ushortOrStringOutputTag.StartIndex - ushortOrStringOutputStart + ushortOrStringOutputTag.Offset)], ushortOrStringOutputTag.StringReverse);
                                    ushortOrStringOutputTag.SetValue(str);
                                }
                            }
                            else
                            {
                                ushortOrStringOutputTag.SetValue(ushortOrStringOutputResult[(ushortOrStringOutputTag.StartIndex - ushortOrStringOutputStart)..(ushortOrStringOutputTag.StartIndex - ushortOrStringOutputStart + ushortOrStringOutputTag.Offset)]);
                            }
                        }
                        outputRegisterStopwatch.Stop();
                    }
                    #endregion
                }



                await base.UpdateTags();
            }
        }

        public sealed override async Task<RequestResult> UpdateTag(Tag tag)
        {
            try
            {
                if (tag is ModbusTCPTag modbusTCPTag)
                {
                    if (RunFlag)
                    {
                        bool output = modbusTCPTag.InputOrOutput;

                        var station = modbusTCPTag.Station;
                        var startIndex = modbusTCPTag.StartIndex;
                        var offset = modbusTCPTag.Offset;
                        switch (tag.DataType)
                        {
                            //bool
                            case 1:
                                bool res_bool = false;
                                if (!output)
                                {
                                    res_bool = (await master?.ReadInputsAsync(station, startIndex, offset)).FirstOrDefault();

                                }
                                else
                                {
                                    res_bool = (await master?.ReadCoilsAsync(station, startIndex, offset)).FirstOrDefault();
                                }
                                return tag.SetValue(res_bool);
                            //ushort
                            case 2:
                                ushort res_ushort = 0;
                                if (!output)
                                {
                                    res_ushort = (await master?.ReadInputRegistersAsync(station, startIndex, offset)).FirstOrDefault();

                                }
                                else
                                {
                                    res_ushort = (await master?.ReadHoldingRegistersAsync(station, startIndex, offset)).FirstOrDefault();
                                }
                                return tag.SetValue(res_ushort);
                            case 4:
                                bool stringReverse = modbusTCPTag.StringReverse;
                                ushort[] tmp_ushort = new ushort[modbusTCPTag.Offset];
                                if (!output)
                                {
                                    tmp_ushort = (await master?.ReadInputRegistersAsync(station, startIndex, offset));

                                }
                                else
                                {
                                    tmp_ushort = (await master?.ReadHoldingRegistersAsync(station, startIndex, offset));
                                }
                                bool b = BitConverter.IsLittleEndian;
                                var strRes = UshortToString(tmp_ushort, b);
                                return tag.SetValue(strRes);
                            case 11:
                                var res_boolArray = Enumerable.Repeat(false, modbusTCPTag.Offset).ToArray();
                                if (!output)
                                {
                                    res_boolArray = await master.ReadInputsAsync(station, startIndex, offset);

                                }
                                else
                                {
                                    res_boolArray = await master.ReadCoilsAsync(station, startIndex, offset);
                                }
                                return tag.SetValue(res_boolArray);
                            case 22:
                                var res_ushortArray = Enumerable.Repeat((ushort)0, modbusTCPTag.Offset).ToArray();
                                if (!output)
                                {
                                    res_ushortArray = await master.ReadInputRegistersAsync(station, startIndex, offset);

                                }
                                else
                                {
                                    res_ushortArray = await master.ReadHoldingRegistersAsync(station, startIndex, offset);
                                }
                                return tag.SetValue(res_ushortArray);
                            default:
                                return new(4, "Not implement yet");
                        }
                    }
                    else
                    {
                        return new(1, $"Machine status {CommonEnumHelper.GetStatusDetail(StatusCode)} is not allow to update tag");
                    }
                }
                else
                {
                    return new(1, $"Tag type error");
                }
            }
            catch (IOException e)
            {
                Disconnect(e.Message);
                return new(4, $"Update tags fail({e.Message})");
            }
            catch (SocketException e)
            {
                Disconnect(e.Message);
                return new(4, $"Update tags fail({e.Message})");
            }
            catch (InvalidOperationException e)
            {
                Disconnect(e.Message);
                return new(4, $"Update tags fail({e.Message})");
            }
            catch (Exception e)
            {
                Error(e.Message);
                return new(4, $"Update tags fail({e.Message})");
            }
        }

        public sealed override async Task<RequestResult> SetTag(Tag tag, object val)
        {
            if (tag is ModbusTCPTag modbusTCPTag)
            {
                var output = modbusTCPTag.InputOrOutput;
                var stringReverse = modbusTCPTag.StringReverse;
                var station = modbusTCPTag.Station;
                var startIndex = modbusTCPTag.StartIndex;
                var offset = modbusTCPTag.Offset;
                switch (tag.DataType)
                {
                    //bool
                    case 1:
                        if (val is bool bool_val)
                        {
                            //bool bool_val = (bool)val;
                            if (output)
                            {
                                await master.WriteSingleCoilAsync((byte)station, (ushort)startIndex, bool_val);
                                bool bool_res = (await master.ReadCoilsAsync(station, startIndex, offset)).FirstOrDefault();
                                var res_bool = tag.SetValue(bool_res);
                                TagsStatechange();
                                return res_bool;
                            }
                            else
                            {
                                return new(4, "Input is not allow to set");
                            }
                        }
                        else
                        {
                            return new(4, "Data is not boolean type");
                        }
                    //ushort
                    case 2:
                        if (val is ushort ushort_val)
                        {
                            //ushort ushort_val = (ushort)val;
                            if (output)
                            {
                                //var a = await master.ReadHoldingRegistersAsync((byte)station, (ushort)startIndex, (byte)offset);
                                await master.WriteSingleRegisterAsync(station, startIndex, ushort_val);
                                ushort ushort_res = (await master.ReadHoldingRegistersAsync((byte)station, (ushort)startIndex, (ushort)offset)).FirstOrDefault();
                                var res_ushort = tag.SetValue(ushort_res);
                                TagsStatechange();
                                return res_ushort;
                            }
                            else
                            {
                                return new(4, "Input is not allow to set");
                            }
                        }
                        else
                        {
                            return new(4, "Data is not ushort type");
                        }
                    case 4:
                        if (val is string string_val)
                        {
                            //string string_val = (string)val;
                            if (output)
                            {
                                ushort[] reset = Enumerable.Repeat((ushort)0, offset).ToArray();
                                await master?.WriteMultipleRegistersAsync(station, startIndex, reset);
                                if (!string.IsNullOrEmpty(string_val))
                                {
                                    var tmp = StringToByte(string_val, stringReverse);
                                    await master.WriteMultipleRegistersAsync(station, startIndex, tmp);
                                }
                                ushort ushort_valres = (await master.ReadHoldingRegistersAsync(station, startIndex, offset)).FirstOrDefault();
                                var res_str = tag.SetValue(Convert.ToChar(ushort_valres).ToString());
                                TagsStatechange();
                                return res_str;
                            }
                            else
                            {
                                return new(4, "Input is not allow to set");
                            }
                        }
                        else
                        {
                            return new(4, "Data is not string type");
                        }
                    case 11:
                        if (val is bool[] boolArr_val)
                        {
                            if (output)
                            {
                                if (boolArr_val.Length != offset)
                                {
                                    return new(4, $"Boolean array length {boolArr_val.Length} and offset {offset} not match");
                                }
                                await master.WriteMultipleCoilsAsync((byte)station, (ushort)startIndex, boolArr_val);
                                bool[] boolArr_res = await master.ReadCoilsAsync(station, startIndex, offset);
                                var res_boolArr = tag.SetValue(boolArr_res);
                                TagsStatechange();
                                return res_boolArr;
                            }
                            else
                            {
                                return new(4, "Input is not allow to set");
                            }
                        }
                        else
                        {
                            return new(4, "Data is not boolean type");
                        }
                    case 22:
                        if (val is ushort[] ushortArr_val)
                        {
                            if (output)
                            {
                                if (ushortArr_val.Length != offset)
                                {
                                    return new(4, $"Ushort array length {ushortArr_val.Length} and offset {offset} not match");
                                }
                                await master.WriteMultipleRegistersAsync((byte)station, (ushort)startIndex, ushortArr_val);
                                ushort[] ushortArr_res = await master.ReadHoldingRegistersAsync(station, startIndex, offset);
                                var res_ushortArr = tag.SetValue(ushortArr_res);
                                TagsStatechange();
                                return res_ushortArr;
                            }
                            else
                            {
                                return new(4, "Input is not allow to set");
                            }
                        }
                        else
                        {
                            return new(4, "Data is not boolean type");
                        }
                    default:
                        return new(3, "Not implement yet");
                }
            }
            else
            {
                return new(4, "casing fail");
            }
        }


        private ushort[] StringToByte(string s, bool reverse)
        {
            List<ushort> tmp = new();
            byte[] byteArr = ASCIIEncoding.ASCII.GetBytes(s);
            if (s.Length % 2 == 0)
            {

            }
            else
            {
                byteArr = byteArr.Append((byte)0x00).ToArray();
            }

            for (int n = 0; n < s.Length; n += 2)
            {
                var byteInterval = byteArr.Skip(n).Take(2).ToArray().AsSpan();
                if (reverse)
                {
                    //var a = (ushort)BinaryPrimitives.ReadInt16BigEndian(byteInterval);
                    tmp.Add((ushort)BinaryPrimitives.ReadInt16BigEndian(byteInterval));
                }
                else
                {
                    //var b = (ushort)BinaryPrimitives.ReadInt16LittleEndian(byteInterval);
                    tmp.Add((ushort)BinaryPrimitives.ReadInt16LittleEndian(byteInterval));
                }
            }
            return tmp.ToArray();
        }

        private string UshortToString(ushort[] ushorts, bool reverse)
        {
            string res = string.Empty;
            bool b = BitConverter.IsLittleEndian;
            foreach (var ushortNum in ushorts)
            {
                var byteArray = BitConverter.GetBytes(ushortNum);
                if (reverse)
                {
                    byteArray = byteArray.Reverse().ToArray();
                }

                string s = Encoding.ASCII.GetString(byteArray.TakeWhile(x => x != 0).ToArray());
                res += s;
            }
            return res;
        }
    }
}
