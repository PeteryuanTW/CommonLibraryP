using QGACTIVEXLib;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SV
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Range(3000, int.MaxValue)]
        public int SVId { get; set; }
        public string Name { get; set; } = null!;
        [NotMapped]
        public SV_DATA_TYPE SV_DATA_TYPE { get; set; }
        private Object? value { get; set; }

        private DateTime? lastestUpdateTime;
        public DateTime? LastestUpdateTime => lastestUpdateTime;

        [NotMapped]
        public Object? Value
        {
            get => value;
            set
            {
                this.value = value;
                lastestUpdateTime = DateTime.Now;
            }
        }

        public string ValueString => value?.ToString() ?? string.Empty;
        public void SetValue(Object? obj)
        {
            if (obj is null) return;
            switch (SV_DATA_TYPE)
            {
                case SV_DATA_TYPE.SV_ASCII_TYPE:
                    Value = obj.ToString();
                    break;
                case SV_DATA_TYPE.SV_BINARY_TYPE:
                    if (byte.TryParse(obj.ToString(), out byte byteValue))
                        Value = byteValue;
                    break;
                case SV_DATA_TYPE.SV_BOOLEAN_TYPE:
                    if (bool.TryParse(obj.ToString(), out bool booleanValue))
                        Value = booleanValue;
                    break;

                case SV_DATA_TYPE.SV_INT_1_TYPE:
                    if (sbyte.TryParse(obj.ToString(), out sbyte sbyteValue))
                        Value = sbyteValue;
                    break;
                case SV_DATA_TYPE.SV_INT_2_TYPE:
                    if (short.TryParse(obj.ToString(), out short shortValue))
                        Value = shortValue;
                    break;
                case SV_DATA_TYPE.SV_INT_4_TYPE:
                    if (int.TryParse(obj.ToString(), out int intValue))
                        Value = intValue;
                    break;

                case SV_DATA_TYPE.SV_UINT_1_TYPE:
                    if (byte.TryParse(obj.ToString(), out byte u1ByteValue))
                        Value = u1ByteValue;
                    break;
                case SV_DATA_TYPE.SV_UINT_2_TYPE:
                    if (ushort.TryParse(obj.ToString(), out ushort ushortValue))
                        Value = ushortValue;
                    break;
                case SV_DATA_TYPE.SV_UINT_4_TYPE:
                    if (uint.TryParse(obj.ToString(), out uint uintValue))
                        Value = uintValue;
                    break;
                case SV_DATA_TYPE.SV_FT_4_TYPE:
                    if (float.TryParse(obj.ToString(), out float floatValue))
                        Value = floatValue;
                    break;
                case SV_DATA_TYPE.SV_FT_8_TYPE:
                    if (double.TryParse(obj.ToString(), out double doubleValue))
                        Value = doubleValue;
                    break;
                default:
                    break;
            }
        }
    }
}
