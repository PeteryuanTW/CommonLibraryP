using CommonLibraryP.SecsGemPKG;
using DevExpress.ClipboardSource.SpreadsheetML;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            var binValues = bins.Select(b => new SecsBinaryValue { BinaryValue = b }).ToList();

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

            var boolValues = bools.Select(b => new SecsBoolValue { BoolValue = b }).ToList();

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

            var sbyteValues = sbytes.Select(b => new SecsI1Value { I1Value = b }).ToList();

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

            var shortValues = shorts.Select(b => new SecsI2Value { I2Value = b }).ToList();

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

            var intValues = ints.Select(b => new SecsI4Value { I4Value = b }).ToList();

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

            var longValues = longs.Select(b => new SecsI8Value { I8Value = b }).ToList();

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

            var byteValues = bytes.Select(b => new SecsU1Value { U1Value = b }).ToList();

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

            var ushortValues = ushorts.Select(b => new SecsU2Value { U2Value = b }).ToList();

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

            var uintValues = uints.Select(b => new SecsU4Value { U4Value = b }).ToList();

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

            var ulongValues = ulongs.Select(b => new SecsU8Value { U8Value = b }).ToList();

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

            var floatValues = floats.Select(b => new SecsF4Value { F4Value = b }).ToList();

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

            var doubleValues = doubles.Select(b => new SecsF8Value { F8Value = b }).ToList();

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

    public enum SecsNodeType
    {
        List,
        Ascii,
        Binary,
        BinaryValue,
        Boolean,
        BooleanValue,
        I1,
        I1Value,
        I2,
        I2Value,
        I4,
        I4Value,
        I8,
        I8Value,
        U1,
        U1Value,
        U2,
        U2Value,
        U4,
        U4Value,
        U8,
        U8Value,
        F4,
        F4Value,
        F8,
        F8Value
    }


    public abstract class SecsTreeNode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? ParentId { get; set; }

        public bool IsRoot => ParentId is null;

        public int Sequence { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [NotMapped]
        public List<SecsTreeNode> ChildrenNode { get; set; } = new List<SecsTreeNode>();

        public abstract bool IsValueType { get; }

        public abstract string CodeOrValueString { get; }

        public SecsTreeNode? FindNodeByName(string name)
        {
            if (Name == name)
                return this;

            foreach (var child in ChildrenNode)
            {
                var found = child.FindNodeByName(name);
                if (found != null)
                    return found;
            }
            return null;
        }
        public bool SetValueByName(string nodeName, object value)
        {
            var target = FindNodeByName(nodeName);
            if(target is null) return false;
            if(!target.IsValueType) return false;
            switch (target)
            {
                case SecsAscii asciiItem when value is string strinfValue:
                    asciiItem.StringValue = strinfValue;
                    return true;
                case SecsBinaryValue binaryItem when value is byte byteValue:
                    binaryItem.BinaryValue = byteValue;
                    return true;
                case SecsI4Value i4Item when value is int i4Value:
                    i4Item.I4Value = i4Value;
                    return true;
                case SecsI8Value i8Item when value is long i8Value:
                    i8Item.I8Value = i8Value;
                    return true;

                case SecsU1Value u1Item when value is byte u1Value:
                    u1Item.U1Value = u1Value;
                    return true;

                case SecsU2Value u2Item when value is ushort u2Value:
                    u2Item.U2Value = u2Value;
                    return true;

                case SecsU4Value u4Item when value is uint u4Value:
                    u4Item.U4Value = u4Value;
                    return true;

                case SecsU8Value u8Item when value is ulong u8Value:
                    u8Item.U8Value = u8Value;
                    return true;

                case SecsF4Value f4Item when value is float f4Value:
                    f4Item.F4Value = f4Value;
                    return true;

                case SecsF8Value f8Item when value is double f8Value:
                    f8Item.F8Value = f8Value;
                    return true;
                case SecsUnknown unknownItem when value is byte rawByte:
                default:
                    return false;
            }


        }
    }


    public interface ISecsValue<T>
    {
        public T Value { get; }
    }

    public class SecsList : SecsTreeNode
    {
        public override string CodeOrValueString => $"<L[{ChildrenNode.Count}]>";

        public override bool IsValueType => false;
    }

    public class SecsAscii : SecsTreeNode, ISecsValue<string>
    {
        public override string CodeOrValueString => $"<A[{StringValue.Length}] {StringValue}>";
        public string StringValue { get; set; } = string.Empty;
        public string Value => StringValue;

        public override bool IsValueType => true;
    }

    public class SecsBinary : SecsTreeNode, ISecsValue<List<byte>>
    {
        public override string CodeOrValueString => $"<B[{Value.Count}]>";

        public List<byte> Value
            => ChildrenNode.OfType<SecsBinaryValue>().Select(x => x.Value)
            .ToList();

        public override bool IsValueType => false;
    }

    public class SecsBinaryValue : SecsTreeNode, ISecsValue<byte>
    {

        public byte Value => BinaryValue;

        public byte BinaryValue { get; set; }

        public override string CodeOrValueString => BinaryValue.ToString();

        public override bool IsValueType => true;
    }

    public class SecsBool : SecsTreeNode, ISecsValue<List<bool>>
    {
        public override string CodeOrValueString => $"<Boolean[{Value.Count}]>";

        public List<bool> Value => ChildrenNode.OfType<SecsBoolValue>().Select(x => x.Value)
            .ToList();

        public override bool IsValueType => false;
    }

    public class SecsBoolValue : SecsTreeNode, ISecsValue<bool>
    {
        public override string CodeOrValueString => BoolValue.ToString();
        public bool Value => BoolValue;
        public bool BoolValue { get; set; }

        public override bool IsValueType => true;
    }
    public class SecsI1 : SecsTreeNode, ISecsValue<List<sbyte>>
    {
        public override string CodeOrValueString => $"<I1[{Value.Count}]>";

        public List<sbyte> Value =>
            ChildrenNode.OfType<SecsI1Value>().Select(x => x.Value)
            .ToList();

        public override bool IsValueType => false;
    }

    public class SecsI1Value : SecsTreeNode, ISecsValue<sbyte>
    {
        public override string CodeOrValueString => I1Value.ToString();
        public sbyte Value => I1Value;
        public sbyte I1Value { get; set; }

        public override bool IsValueType => true;
    }

    public class SecsI2 : SecsTreeNode, ISecsValue<List<short>>
    {
        public override string CodeOrValueString => $"<I2{Value.Count}>";

        public List<short> Value =>
            ChildrenNode.OfType<SecsI2Value>().Select(x => x.Value)
            .ToList();

        public override bool IsValueType => false;
    }

    public class SecsI2Value : SecsTreeNode, ISecsValue<short>
    {
        public override string CodeOrValueString => I2Value.ToString();
        public short Value => I2Value;
        public short I2Value { get; set; }

        public override bool IsValueType => true;
    }

    public class SecsI4 : SecsTreeNode, ISecsValue<List<int>>
    {
        public override string CodeOrValueString => $"<I4[{Value.Count}]>";

        public List<int> Value =>
            ChildrenNode.OfType<SecsI4Value>().Select(x => x.Value)
            .ToList();

        public override bool IsValueType => false;
    }

    public class SecsI4Value : SecsTreeNode, ISecsValue<int>
    {
        public override string CodeOrValueString => I4Value.ToString();
        public int Value => I4Value;
        public int I4Value { get; set; }

        public override bool IsValueType => true;
    }


    public class SecsI8 : SecsTreeNode, ISecsValue<List<long>>
    {
        public override string CodeOrValueString => $"<I8{Value.Count}>";

        public List<long> Value =>
            ChildrenNode.OfType<SecsI8Value>().Select(x => x.Value)
            .ToList();

        public override bool IsValueType => false;
    }

    public class SecsI8Value : SecsTreeNode, ISecsValue<long>
    {
        public override string CodeOrValueString => I8Value.ToString();
        public long Value => I8Value;
        public long I8Value { get; set; }

        public override bool IsValueType => true;
    }

    public class SecsU1 : SecsTreeNode, ISecsValue<List<byte>>
    {
        public override string CodeOrValueString => $"<U1[{Value.Count}]>";

        public List<byte> Value =>
            ChildrenNode.OfType<SecsU1Value>().Select(x => x.Value)
            .ToList();

        public override bool IsValueType => false;
    }

    public class SecsU1Value : SecsTreeNode, ISecsValue<byte>
    {
        public override string CodeOrValueString => U1Value.ToString();
        public byte Value => U1Value;
        public byte U1Value { get; set; }

        public override bool IsValueType => true;
    }

    public class SecsU2 : SecsTreeNode, ISecsValue<List<ushort>>
    {
        public override string CodeOrValueString => $"<U2[{Value.Count}]>";

        public List<ushort> Value =>
            ChildrenNode.OfType<SecsU2Value>().Select(x => x.Value)
            .ToList();

        public override bool IsValueType => false;
    }

    public class SecsU2Value : SecsTreeNode, ISecsValue<ushort>
    {
        public override string CodeOrValueString => U2Value.ToString();
        public ushort Value => U2Value;
        public ushort U2Value { get; set; }

        public override bool IsValueType => true;
    }

    public class SecsU4 : SecsTreeNode, ISecsValue<List<uint>>
    {
        public override string CodeOrValueString => $"<U4[{Value.Count}]>";

        public List<uint> Value =>
            ChildrenNode.OfType<SecsU4Value>().Select(x => x.Value)
            .ToList();

        public override bool IsValueType => false;
    }

    public class SecsU4Value : SecsTreeNode, ISecsValue<uint>
    {
        public override string CodeOrValueString => U4Value.ToString();
        public uint Value => U4Value;
        public uint U4Value { get; set; }

        public override bool IsValueType => true;
    }

    public class SecsU8 : SecsTreeNode, ISecsValue<List<ulong>>
    {
        public override string CodeOrValueString => $"<U8[{Value.Count}]>";
        public List<ulong> Value =>
            ChildrenNode.OfType<SecsU8Value>().Select(x => x.Value)
            .ToList();

        public override bool IsValueType => false;
    }

    public class SecsU8Value : SecsTreeNode, ISecsValue<ulong>
    {
        public override string CodeOrValueString => U8Value.ToString();
        public ulong Value => U8Value;
        public ulong U8Value { get; set; }

        public override bool IsValueType => true;
    }

    public class SecsF4 : SecsTreeNode, ISecsValue<List<float>>
    {
        public override string CodeOrValueString => $"<F4[{Value.Count}]>";

        public List<float> Value =>
            ChildrenNode.OfType<SecsF4Value>().Select(x => x.Value)
            .ToList();

        public override bool IsValueType => false;
    }

    public class SecsF4Value : SecsTreeNode, ISecsValue<float>
    {
        public override string CodeOrValueString => F4Value.ToString();
        public float Value => F4Value;
        public float F4Value { get; set; }

        public override bool IsValueType => true;
    }

    public class SecsF8 : SecsTreeNode, ISecsValue<List<double>>
    {
        public override string CodeOrValueString => $"<F8[{Value.Count}]>";

        public List<double> Value =>
            ChildrenNode.OfType<SecsF8Value>().Select(x => x.Value)
            .ToList();

        public override bool IsValueType => false;
    }

    public class SecsF8Value : SecsTreeNode, ISecsValue<double>
    {
        public override string CodeOrValueString => F8Value.ToString();
        public double Value => F8Value;
        public double F8Value { get; set; }

        public override bool IsValueType => true;
    }

    public class SecsUnknown(byte b) : SecsTreeNode, ISecsValue<byte>
    {
        private byte rawByte = b;
        public byte Value => rawByte;

        public override string CodeOrValueString => $"Unknown({rawByte.ToString()})";

        public override bool IsValueType => true;
    }
    #endregion

}
