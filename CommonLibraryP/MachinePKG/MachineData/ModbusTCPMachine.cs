using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using NModbus;
using System.Text;
using System.Threading.Tasks;
using CommonLibraryP.API;
using System.Buffers.Binary;
using CommonLibraryP.Data;

namespace CommonLibraryP.MachinePKG
{
    public class ModbusTCPMachine : Machine
    {
        private TcpClient tcpClient;
        private IModbusFactory modbusFactory;
        public IModbusMaster? master;
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
                                string strList = string.Empty;
                                bool b = BitConverter.IsLittleEndian;
                                foreach (var twoUshort in tmp_ushort)
                                {
                                    var byteArray = BitConverter.GetBytes(twoUshort);
                                    if (stringReverse)
                                    {
                                        byteArray = byteArray.Reverse().ToArray();
                                    }

                                    string s = Encoding.ASCII.GetString(byteArray.TakeWhile(x => x != 0).ToArray());
                                    strList += s;
                                }
                                return tag.SetValue(strList);
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
    }
}
