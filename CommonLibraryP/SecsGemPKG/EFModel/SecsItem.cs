using CommonLibraryP.SecsGemPKG;
using DevExpress.ClipboardSource.SpreadsheetML;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommonLibraryP.SecsGemPKG.SecsBool;
using static System.Reflection.Metadata.BlobBuilder;

namespace CommonLibraryP.SecsGemPKG
{
    public static class SecsParser
    {
        public static SecsTreeNode Parse(Object rawObject)
        {
            if (rawObject is byte[] rawBytes)
            {
                if (rawBytes.IsNullOrEmpty())
                {
                    return new SecsList();
                }
                try
                {
                    int index = 0;
                    var res = ParseItem(rawBytes, ref index);

                    return res;
                }
                catch (Exception ex)
                {
                    throw new InvalidCastException($"Parsing byte[] fail({ex.Message})");
                }
            }
            else
            {
                throw new InvalidCastException("Object is not byte[] type");
            }
        }

        private static SecsTreeNode ParseItem(byte[] data, ref int index)
        {
            int formatStart = index;
            byte formatByte = data[formatStart];
            byte formatCode = (byte)(formatByte >> 2);
            int lengthBytes = formatByte & 0x03;

            int lengthStart = formatStart + 1;
            int itemLength = 0;
            for (int i = 0; i < lengthBytes; i++)
                itemLength = (itemLength << 8) | data[lengthStart + i];
            Console.WriteLine($"[ParseItem] index: {index}, formatCode: 0x{formatCode:X2}, length:{itemLength}");
            index = lengthStart + lengthBytes;

            switch (formatCode)
            {
                case 0x00:
                    return ParseList(data, ref index, itemLength);     // L
                case 0x08:
                    return ParseBinary(data, ref index, itemLength);   // B
                case 0x10:
                    return ParseAscii(data, ref index, itemLength);    // A
                //case 0x18:
                //    return ParseI1(data, ref index, itemLength);
                case 0x19:
                    return ParseI1(data, ref index, itemLength);       // I1
                case 0x1A:
                    return ParseI2(data, ref index, itemLength);
                case 0x1B:
                    return ParseI8(data, ref index, itemLength);       // I8
                //case 0x1C:
                //    return ParseU1(data, ref index, itemLength);       // U1
                //case 0x1D:
                //    return ParseU2(data, ref index, itemLength);       // U2
                //case 0x1E:
                //    return ParseU4(data, ref index, itemLength);       // U4
                //case 0x1F:
                //    return ParseU8(data, ref index, itemLength);       // U8
                case 0x20:
                    return ParseF8(data, ref index, itemLength);       // F8
                case 0x24:
                    return ParseF4(data, ref index, itemLength);       // F4

                //
                case 0x29:
                    return ParseU1(data, ref index, itemLength);
                case 0x2A:
                    return ParseU2(data, ref index, itemLength);       // U2 fallback
                case 0x2C:
                    return ParseU4(data, ref index, itemLength);       // U4 fallback
                case 0x2D:
                    return ParseU8(data, ref index, itemLength);       // U8 fallback
                                                                       // U1 fallback

                default:
                    return new SecsUnknown(formatCode);
            }



        }

        //public static string ToSecsFormatString(ISecsItem item, int indentLevel = 0)
        //{
        //    string indent = new string(' ', indentLevel * 2);

        //    if (item is SecsList list)
        //    {
        //        var sb = new StringBuilder();
        //        sb.AppendLine($"{indent}<L[{list.Items.Count}]");
        //        foreach (var subItem in list.Items)
        //        {
        //            sb.AppendLine(ToSecsFormatString(subItem, indentLevel + 1));
        //        }
        //        sb.Append($"{indent}>");
        //        return sb.ToString();
        //    }
        //    else
        //    {
        //        return $"{indent}{item.DisplayValue()}";
        //    }
        //}


        #region byte array parsing to objects

        private static SecsTreeNode ParseList(byte[] data, ref int index, int count)
        {
            var secsList = new SecsList();
            for (int i = 0; i < count; i++)
                secsList.ChildrenNode.Add(ParseItem(data, ref index));

            return secsList;
        }

        private static SecsTreeNode ParseAscii(byte[] data, ref int index, int length)
        {
            var str = Encoding.ASCII.GetString(data, index, length);
            index += length;
            //Console.WriteLine($"[Ascii] index after={index}");
            var res = new SecsAscii
            {
                StringValue = str,
            };
            //res.SetValuesFromCode(str);
            return res;
        }

        private static SecsTreeNode ParseBinary(byte[] data, ref int index, int length)
        {
            var bins = new byte[length];
            Array.Copy(data, index, bins, 0, length);
            index += length;

            var binValues = bins.Select(b => new SecsBinaryValue { Value = b }).ToList();

            var res = new SecsBinary()
            {
                ChildrenNode = binValues.Cast<SecsTreeNode>().ToList()
            };
            //res.SetValuesFromCode(bin);
            //Console.WriteLine(res.DisplayValue());
            return res;
        }

        private static SecsTreeNode ParseBool(byte[] data, ref int index, int length)
        {
            var bools = new List<bool>();
            for (int i = 0; i < length; i++)
                bools.Add(data[index++] != 0);
            //Console.WriteLine($"[Bool] index after={index}");

            var boolValues = bools.Select(b => new SecsBoolValue { Value = b }).ToList();

            var res = new SecsBool()
            {
                ChildrenNode = boolValues.Cast<SecsTreeNode>().ToList()
            };
            //Console.WriteLine(res.DisplayValue());
            return res;
        }

        private static SecsTreeNode ParseI1(byte[] data, ref int index, int length)
        {
            var sbytes = new List<sbyte>();
            for (int i = 0; i < length; i++)
                sbytes.Add((sbyte)data[index++]);
            //Console.WriteLine($"[I1] index after={index}");

            var sbyteValues = sbytes.Select(b => new SecsI1Value { Value = b }).ToList();

            var res = new SecsI1()
            {
                ChildrenNode = sbyteValues.Cast<SecsTreeNode>().ToList()
            };
            //Console.WriteLine(res.DisplayValue());
            return res;
        }

        private static SecsTreeNode ParseI2(byte[] data, ref int index, int length)
        {
            var shorts = new List<short>();
            for (int i = 0; i < length / 2; i++)
            {
                short val = (short)((data[index] << 8) | data[index + 1]);
                shorts.Add(val);
                index += 2;
            }
            //Console.WriteLine($"[I2] index after={index}");

            var shortValues = shorts.Select(b => new SecsI2Value { Value = b }).ToList();

            var res = new SecsI2()
            {
                ChildrenNode = shortValues.Cast<SecsTreeNode>().ToList()
            };

            return res;
        }

        private static SecsTreeNode ParseI4(byte[] data, ref int index, int length)
        {
            var ints = new List<int>();
            for (int i = 0; i < length / 4; i++)
            {
                int val = (data[index] << 24) | (data[index + 1] << 16) | (data[index + 2] << 8) | data[index + 3];
                ints.Add(val);
                index += 4;
            }
            //Console.WriteLine($"[I4] index after={index}");

            var intValues = ints.Select(b => new SecsI4Value { Value = b }).ToList();

            var res = new SecsI4()
            {
                ChildrenNode = intValues.Cast<SecsTreeNode>().ToList()
            };
            //Console.WriteLine(res.DisplayValue());
            return res;
        }

        private static SecsTreeNode ParseI8(byte[] data, ref int index, int length)
        {
            var longs = new List<long>();
            for (int i = 0; i < length / 8; i++)
            {
                byte[] bytes = new byte[8];
                for (int j = 0; j < 8; j++)
                    bytes[7 - j] = data[index + j]; // big-endian
                long val = BitConverter.ToInt64(bytes, 0);
                longs.Add(val);
                index += 8;
            }
            //Console.WriteLine($"[I8] index after={index}");

            var longValues = longs.Select(b => new SecsI8Value { Value = b }).ToList();

            var res = new SecsI8()
            {
                ChildrenNode = longValues.Cast<SecsTreeNode>().ToList()
            };
            //Console.WriteLine(res.DisplayValue());
            return res;
        }

        private static SecsTreeNode ParseU1(byte[] data, ref int index, int length)
        {
            var bytes = new List<byte>();
            for (int i = 0; i < length; i++)
                bytes.Add(data[index++]);

            var byteValues = bytes.Select(b => new SecsU1Value { Value = b }).ToList();

            var res = new SecsU1()
            {
                ChildrenNode = byteValues.Cast<SecsTreeNode>().ToList()
            };
            return res;
        }

        private static SecsTreeNode ParseU2(byte[] data, ref int index, int length)
        {
            var ushorts = new List<ushort>();
            for (int i = 0; i < length / 2; i++)
            {
                ushort val = (ushort)((data[index] << 8) | data[index + 1]);
                ushorts.Add(val);
                index += 2;
            }
            //Console.WriteLine($"[U2] index after={index}");

            var ushortValues = ushorts.Select(b => new SecsU2Value { Value = b }).ToList();

            var res = new SecsU2()
            {
                ChildrenNode = ushortValues.Cast<SecsTreeNode>().ToList()
            };
            //Console.WriteLine(res.DisplayValue());
            return res;
        }

        private static SecsTreeNode ParseU4(byte[] data, ref int index, int length)
        {
            var uints = new List<uint>();
            for (int i = 0; i < length / 4; i++)
            {
                uint val = (uint)((data[index] << 24) | (data[index + 1] << 16) | (data[index + 2] << 8) | data[index + 3]);
                uints.Add(val);
                index += 4;
            }
            //Console.WriteLine($"[U4] index after={index}");

            var uintValues = uints.Select(b => new SecsU4Value { Value = b }).ToList();

            var res = new SecsU4()
            {
                ChildrenNode = uintValues.Cast<SecsTreeNode>().ToList()
            };
            //Console.WriteLine(res.DisplayValue());
            return res;
        }

        private static SecsTreeNode ParseU8(byte[] data, ref int index, int length)
        {
            var ulongs = new List<ulong>();
            for (int i = 0; i < length / 8; i++)
            {
                byte[] bytes = new byte[8];
                for (int j = 0; j < 8; j++)
                    bytes[7 - j] = data[index + j]; // big-endian
                ulong val = BitConverter.ToUInt64(bytes, 0);
                ulongs.Add(val);
                index += 8;
            }
            //Console.WriteLine($"[U8] index after={index}");

            var ulongValues = ulongs.Select(b => new SecsU8Value { Value = b }).ToList();

            var res = new SecsU8()
            {
                ChildrenNode = ulongValues.Cast<SecsTreeNode>().ToList()
            };
            //Console.WriteLine(res.DisplayValue());
            return res;
        }

        private static SecsTreeNode ParseF4(byte[] data, ref int index, int length)
        {
            var floats = new List<float>();
            for (int i = 0; i < length / 4; i++)
            {
                byte[] bytes = new byte[4];
                for (int j = 0; j < 4; j++)
                    bytes[3 - j] = data[index + j]; // big-endian
                float val = BitConverter.ToSingle(bytes, 0);
                floats.Add(val);
                index += 4;
            }
            //Console.WriteLine($"[F4] index after={index}");

            var floatValues = floats.Select(b => new SecsF4Value { Value = b }).ToList();

            var res = new SecsF4()
            {
                ChildrenNode = floatValues.Cast<SecsTreeNode>().ToList()
            };
            //Console.WriteLine(res.DisplayValue());
            return res;
        }

        private static SecsTreeNode ParseF8(byte[] data, ref int index, int length)
        {
            var doubles = new List<double>();
            int count = length / 8;
            for (int i = 0; i < count; i++)
            {
                byte[] bytes = new byte[8];
                for (int j = 0; j < 8; j++)
                    bytes[7 - j] = data[index + j]; // big-endian
                double val = BitConverter.ToDouble(bytes, 0);
                doubles.Add(val);
                index += 8;
            }
            //Console.WriteLine($"[F8] index after={index}");

            var doubleValues = doubles.Select(b => new SecsF8Value { Value = b }).ToList();

            var res = new SecsF8()
            {
                ChildrenNode = doubleValues.Cast<SecsTreeNode>().ToList()
            };
            return res;
        }

        #endregion


        #region object parsing to byte array

        public static byte[] EncodeItem(SecsTreeNode item)
        {
            if (item is SecsList list)
                return EncodeList(list);

            byte formatCode = GetFormatCode(item);
            byte[] dataBytes = EncodeData(item);
            byte[] lengthBytes = EncodeLength(dataBytes.Length);
            byte formatByte = (byte)((formatCode << 2) | (lengthBytes.Length & 0x03));

            var result = new List<byte> { formatByte };
            result.AddRange(lengthBytes);
            result.AddRange(dataBytes);
            return result.ToArray();

        }

        private static byte GetFormatCode(SecsTreeNode item)
        {
            return item switch
            {
                SecsList => 0x00,
                SecsBinary => 0x08,
                SecsAscii => 0x10,
                SecsI1 => 0x19,
                SecsI2 => 0x1A,
                SecsI8 => 0x1B,
                SecsF8 => 0x20,
                SecsF4 => 0x24,
                SecsU1 => 0x29,
                SecsU2 => 0x2A,
                SecsU4 => 0x2C,
                SecsU8 => 0x2D,
                _ => throw new NotSupportedException($"Unsupported item type: {item.GetType().Name}")
            };
        }

        private static byte[] EncodeLength(int length)
        {
            if (length <= 0xFF)
                return new byte[] { (byte)length };
            else if (length <= 0xFFFF)
                return BitConverter.GetBytes((ushort)length).Reverse().ToArray();
            else
                return BitConverter.GetBytes(length).Reverse().ToArray();

        }

        private static byte[] EncodeData(SecsTreeNode secsTreeNode)
        {
            return secsTreeNode switch
            {
                SecsBinary b => b.Value.ToArray(),
                SecsAscii a => Encoding.ASCII.GetBytes(a.StringValue),
                SecsI1 i => i.Value.Select(b => (byte)b).ToArray(),
                SecsI2 i => i.Value.SelectMany(v => BitConverter.GetBytes(v).Reverse()).ToArray(),
                SecsI4 i => i.Value.SelectMany(v => BitConverter.GetBytes(v).Reverse()).ToArray(),
                SecsI8 i => i.Value.SelectMany(v => BitConverter.GetBytes(v).Reverse()).ToArray(),
                SecsU1 u => u.Value.ToArray(),
                SecsU2 u => u.Value.SelectMany(v => BitConverter.GetBytes(v).Reverse()).ToArray(),
                SecsU4 u => u.Value.SelectMany(v => BitConverter.GetBytes(v).Reverse()).ToArray(),
                SecsU8 u => u.Value.SelectMany(v => BitConverter.GetBytes(v).Reverse()).ToArray(),
                SecsF4 f => f.Value.SelectMany(v => BitConverter.GetBytes(v).Reverse()).ToArray(),
                SecsF8 f => f.Value.SelectMany(v => BitConverter.GetBytes(v).Reverse()).ToArray(),
                SecsBool b => b.Value.Select(v => (byte)(v ? 1 : 0)).ToArray(),
                _ => throw new NotSupportedException($"Unsupported item type: {secsTreeNode.GetType().Name}")
            };

        }

        private static byte[] EncodeList(SecsList list)
        {
            var children = list.ChildrenNode.Select(EncodeItem).ToList();
            byte[] lengthBytes = EncodeLength(children.Count);
            byte formatByte = (byte)((0x00 << 2) | (lengthBytes.Length & 0x03));

            var result = new List<byte> { formatByte };
            result.AddRange(lengthBytes);
            foreach (var child in children)
                result.AddRange(child);
            return result.ToArray();
        }



        #endregion
    }


    #region item classes

    public enum SecsValueSource
    {
        None,
        Code,
        Constant,
        Machine,
    }

    public abstract class SecsTreeNode
    {
        public Guid Id { get; set; }

        public Guid? ParentId { get; set; }

        [NotMapped]
        public List<SecsTreeNode> ChildrenNode { get; set; } = new List<SecsTreeNode>();
    }

    public interface ISecsItem
    {
        public string Code { get; }
    }

    public interface ISecsValue<T>
    {
        public T Value { get; }
    }

    public class SecsList : SecsTreeNode, ISecsItem
    {
        public string Code => "L";
    }

    public class SecsAscii : SecsTreeNode, ISecsItem, ISecsValue<string>
    {
        public string StringValue { get; set; } = string.Empty;

        public string Code => "A";

        [NotMapped]
        public string Value => StringValue;
    }

    public class SecsBinary : SecsTreeNode, ISecsItem, ISecsValue<List<byte>>
    {
        public string Code => "B";

        public List<byte> Value
            => ChildrenNode.OfType<SecsBinaryValue>().Select(x => x.Value)
            .ToList();
    }

    public class SecsBinaryValue : SecsTreeNode, ISecsValue<byte>
    {
        public byte Value { get; set; }
    }

    public class SecsBool : SecsTreeNode, ISecsItem, ISecsValue<List<bool>>
    {
        public string Code => "Boolean";

        public List<bool> Value { get; set; } = new();
    }

    public class SecsBoolValue : SecsTreeNode, ISecsValue<bool>
    {
        public bool Value { get; set; }
    }
    public class SecsI1 : SecsTreeNode, ISecsItem, ISecsValue<List<sbyte>>
    {
        public string Code => "I1";

        public List<sbyte> Value =>
            ChildrenNode.OfType<SecsI1Value>().Select(x => x.Value)
            .ToList();
    }

    public class SecsI1Value : SecsTreeNode, ISecsValue<sbyte>
    {
        public sbyte Value { get; set; }
    }

    public class SecsI2 : SecsTreeNode, ISecsItem, ISecsValue<List<short>>
    {
        public string Code => "I2";

        public List<short> Value =>
            ChildrenNode.OfType<SecsI2Value>().Select(x => x.Value)
            .ToList();
    }

    public class SecsI2Value : SecsTreeNode, ISecsValue<short>
    {
        public short Value { get; set; }
    }

    public class SecsI4 : SecsTreeNode, ISecsItem, ISecsValue<List<int>>
    {
        public string Code => "I4";

        public List<int> Value =>
            ChildrenNode.OfType<SecsI4Value>().Select(x => x.Value)
            .ToList();
    }

    public class SecsI4Value : SecsTreeNode, ISecsValue<int>
    {
        public int Value { get; set; }
    }


    public class SecsI8 : SecsTreeNode, ISecsItem, ISecsValue<List<long>>
    {
        public string Code => "I8";

        public List<long> Value =>
            ChildrenNode.OfType<SecsI8Value>().Select(x => x.Value)
            .ToList();
    }

    public class SecsI8Value : SecsTreeNode, ISecsValue<long>
    {
        public long Value { get; set; }
    }

    public class SecsU1 : SecsTreeNode, ISecsItem, ISecsValue<List<byte>>
    {
        public string Code => "U1";

        public List<byte> Value =>
            ChildrenNode.OfType<SecsU1Value>().Select(x => x.Value)
            .ToList();
    }

    public class SecsU1Value : SecsTreeNode, ISecsValue<byte>
    {
        public byte Value { get; set; }
    }

    public class SecsU2 : SecsTreeNode, ISecsItem, ISecsValue<List<ushort>>
    {
        public string Code => "U2";

        public List<ushort> Value =>
            ChildrenNode.OfType<SecsU2Value>().Select(x => x.Value)
            .ToList();
    }

    public class SecsU2Value : SecsTreeNode, ISecsValue<ushort>
    {
        public ushort Value { get; set; }
    }

    public class SecsU4 : SecsTreeNode, ISecsItem, ISecsValue<List<uint>>
    {
        public string Code => "U4";

        public List<uint> Value =>
            ChildrenNode.OfType<SecsU4Value>().Select(x => x.Value)
            .ToList();
    }

    public class SecsU4Value : SecsTreeNode, ISecsValue<uint>
    {
        public uint Value { get; set; }
    }

    public class SecsU8 : SecsTreeNode, ISecsItem, ISecsValue<List<ulong>>
    {
        public string Code => "U8";
        public List<ulong> Value =>
            ChildrenNode.OfType<SecsU8Value>().Select(x => x.Value)
            .ToList();
    }

    public class SecsU8Value : SecsTreeNode, ISecsValue<ulong>
    {
        public ulong Value { get; set; }
    }

    public class SecsF4 : SecsTreeNode, ISecsItem, ISecsValue<List<float>>
    {
        public string Code => "F4";

        public List<float> Value =>
            ChildrenNode.OfType<SecsF4Value>().Select(x => x.Value)
            .ToList();
    }

    public class SecsF4Value : SecsTreeNode, ISecsValue<float>
    {
        public float Value { get; set; }
    }

    public class SecsF8 : SecsTreeNode, ISecsItem, ISecsValue<List<double>>
    {
        public string Code => "F8";

        public List<double> Value =>
            ChildrenNode.OfType<SecsF8Value>().Select(x => x.Value)
            .ToList();
    }

    public class SecsF8Value : SecsTreeNode, ISecsValue<double>
    {
        public double Value { get; set; }
    }

    public class SecsUnknown(byte b) : SecsTreeNode, ISecsItem, ISecsValue<byte>
    {
        private byte rawByte = b;
        public byte Value => rawByte;
        public string Code => "Unknown";
    }
    #endregion

}
