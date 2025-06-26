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
        public ModbusTCPMachine(Machine machine) : base(machine)
        {
            tcpClient = new();
            modbusFactory = new ModbusFactory();
        }

        public override async Task ConnectAsync()
        {
            try
            {
                retryCount++;
                TryConnecting();
                tcpClient.Close();
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(Ip, Port);
                master = modbusFactory.CreateMaster(tcpClient);
                Running();
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
        public override async Task<RequestResult> UpdateTag(Tag tag)
        {
            try
            {
                if (MachineStatus != Status.Disconnect && MachineStatus != Status.TryConnecting)
                {
                    int station = tag.Int1;
                    int startIndex = tag.Int2;
                    int offset = tag.Int3;
                    switch (tag.DataType)
                    {
                        //bool
                        case 1:
                            bool res_bool = false;
                            if (tag.Bool2)
                            {
                                res_bool = (await master?.ReadInputsAsync((byte)station, (ushort)startIndex, (ushort)offset)).FirstOrDefault();

                            }
                            else
                            {
                                res_bool = (await master?.ReadCoilsAsync((byte)station, (ushort)startIndex, (ushort)offset)).FirstOrDefault();
                            }
                            await Task.Delay(30);
                            return tag.SetValue(res_bool);
                        //ushort
                        case 2:
                            ushort res_ushort = 0;
                            if (!tag.Bool1)
                            {
                                res_ushort = (await master?.ReadInputRegistersAsync((byte)station, (ushort)startIndex, (ushort)offset)).FirstOrDefault();

                            }
                            else
                            {
                                res_ushort = (await master?.ReadHoldingRegistersAsync((byte)station, (ushort)startIndex, (ushort)offset)).FirstOrDefault();
                            }
                            await Task.Delay(30);
                            return tag.SetValue(res_ushort);
                        case 4:
                            ushort[] tmp_ushort = new ushort[offset];
                            if (!tag.Bool1)
                            {
                                tmp_ushort = (await master?.ReadInputRegistersAsync((byte)station, (ushort)startIndex, (ushort)offset));

                            }
                            else
                            {
                                tmp_ushort = (await master?.ReadHoldingRegistersAsync((byte)station, (ushort)startIndex, (ushort)offset));
                            }
                            string strList = string.Empty;
                            bool b = BitConverter.IsLittleEndian;
                            foreach (var twoUshort in tmp_ushort)
                            {
                                var byteArray = BitConverter.GetBytes(twoUshort);
                                if (tag.Bool2)
                                {
                                    byteArray = byteArray.Reverse().ToArray();
                                }
                                string s = Encoding.ASCII.GetString(byteArray.TakeWhile(x => x != 0).ToArray());
                                strList += s;
                            }
                            await Task.Delay(30);
                            return tag.SetValue(strList);
                        case 11:
                            List<bool> res_boolList = Enumerable.Repeat(false, offset).ToList();
                            if (!tag.Bool1)
                            {
                                res_boolList = (await master.ReadInputsAsync((byte)station, (ushort)startIndex, (ushort)offset)).ToList();

                            }
                            else
                            {
                                res_boolList = (await master.ReadCoilsAsync((byte)station, (ushort)startIndex, (ushort)offset)).ToList();
                            }
                            await Task.Delay(30);
                            return tag.SetValue(res_boolList);
                        case 22:
                            List<ushort> res_ushortList = Enumerable.Repeat((ushort)0, offset).ToList();
                            if (!tag.Bool1)
                            {
                                res_ushortList = (await master.ReadInputRegistersAsync((byte)station, (ushort)startIndex, (ushort)offset)).ToList();

                            }
                            else
                            {
                                res_ushortList = (await master.ReadHoldingRegistersAsync((byte)station, (ushort)startIndex, (ushort)offset)).ToList();
                            }
                            await Task.Delay(30);
                            return tag.SetValue(res_ushortList);
                        default:
                            await Task.Delay(30);
                            return new(4, "Not implement yet");
                    }
                }
                else
                {
                    return new(1, $"Machine status {Status.Disconnect} or {Status.TryConnecting} is not allow to update tag");
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
                var a = e.GetType();
                Error(e.Message);
                return new(4, $"Update tags fail({e.Message})");
            }
        }

        public override async Task<RequestResult> SetTag(Tag tag, object val)
        {
            int station = tag.Int1;
            int startIndex = tag.Int2;
            int offset = tag.Int3;
            switch (tag.DataType)
            {
                //bool
                case 1:
                    if (val is bool bool_val)
                    {
                        //bool bool_val = (bool)val;
                        if (tag.Bool1)
                        {
                            await master.WriteSingleCoilAsync((byte)station, (ushort)startIndex, bool_val);
                            bool bool_res = (await master.ReadCoilsAsync((byte)station, (ushort)startIndex, (ushort)offset)).FirstOrDefault();
                            var res_bool = tag.SetValue(bool_res);
                            TagsStatechange();
                            await Task.Delay(30);
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
                        if (tag.Bool1)
                        {
                            //var a = await master.ReadHoldingRegistersAsync((byte)station, (ushort)startIndex, (byte)offset);
                            await master.WriteSingleRegisterAsync((byte)station, (ushort)startIndex, ushort_val);
                            ushort ushort_res = (await master.ReadHoldingRegistersAsync((byte)station, (ushort)startIndex, (ushort)offset)).FirstOrDefault();
                            var res_ushort = tag.SetValue(ushort_res);
                            TagsStatechange();
                            await Task.Delay(30);
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
                        if (tag.Bool1)
                        {
                            ushort[] reset = Enumerable.Repeat((ushort)0, offset).ToArray();
                            await master?.WriteMultipleRegistersAsync((byte)station, (ushort)startIndex, reset);
                            if (!string.IsNullOrEmpty(string_val))
                            {
                                var tmp = StringToByte(string_val, tag.Bool2);
                                await master.WriteMultipleRegistersAsync((byte)station, (ushort)startIndex, tmp);
                            }
                            ushort ushort_valres = (await master.ReadHoldingRegistersAsync((byte)station, (ushort)startIndex, (ushort)offset)).FirstOrDefault();
                            var res_str = tag.SetValue(Convert.ToChar(ushort_valres).ToString());
                            TagsStatechange();
                            await Task.Delay(30);
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
                    return new(3, "Not implement yet");
                case 22:
                    return new(3, "Not implement yet");
                case 33:
                    return new(3, "Not implement yet");
                default:
                    return new(3, "Not implement yet");
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
