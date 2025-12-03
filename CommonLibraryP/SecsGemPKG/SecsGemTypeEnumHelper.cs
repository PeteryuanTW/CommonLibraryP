using CommonLibraryP.Data;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QSACTIVEXLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public static class SecsGemTypeEnumHelper
    {

        public static IEnumerable<COMMMODETypeWrapperClass> CommmodeTypeWrapperClasses() => Enum.GetValues<COMMMODE>()
            .Select(c => new COMMMODETypeWrapperClass(c));

        public static IEnumerable<SECSCOMMMODETypeWrapperClass> SecsCommmodeTypeWrapperClasses() => Enum.GetValues<SECS_COMM_MODE>()
            .Select(c => new SECSCOMMMODETypeWrapperClass(c));

        public static IEnumerable<HSMSCOMMMODETypeWrapperClass> HsmsCommmodeTypeWrapperClasses() => Enum.GetValues<HSMS_COMM_MODE>()
            .Select(c => new HSMSCOMMMODETypeWrapperClass(c));

        public static IEnumerable<SecsDataTypeWrapperClass> SecsDataTypeWrapperClasses() => Enum.GetValues<SecsDataTypeEnum>()
            .Select(c => new SecsDataTypeWrapperClass(c));

        public static IEnumerable<SecsDataTypeWrapperClass> GetSecsNodeDataTypeWrapperClasses() => SecsDataTypeWrapperClasses().Where(x => x.Index > 0 && x.Index < 20);

        public static SecsTreeNode GetSecsTreeNodeTypeByEnum(SecsDataTypeEnum secsDataTypeEnum)
        {
            return secsDataTypeEnum switch
            {
                SecsDataTypeEnum.List => new SecsList(),
                
                SecsDataTypeEnum.ASCII => new SecsAscii(),

                SecsDataTypeEnum.Boolean => new SecsBool(),

                SecsDataTypeEnum.Binary => new SecsBinary(),

                SecsDataTypeEnum.I1 => new SecsI1(),
                SecsDataTypeEnum.I2 => new SecsI2(),
                SecsDataTypeEnum.I4 => new SecsI4(),
                SecsDataTypeEnum.I8 => new SecsI8(),

                SecsDataTypeEnum.U1 => new SecsU1(),
                SecsDataTypeEnum.U2 => new SecsU2(),
                SecsDataTypeEnum.U4 => new SecsU4(),
                SecsDataTypeEnum.U8 => new SecsU8(),

                SecsDataTypeEnum.F4 => new SecsF4(),
                SecsDataTypeEnum.F8 => new SecsF8(),

                SecsDataTypeEnum.BooleanValue => new SecsBoolValue(),

                SecsDataTypeEnum.BinaryValue => new SecsBinaryValue(),

                SecsDataTypeEnum.I1Value => new SecsI1Value(),
                SecsDataTypeEnum.I2Value => new SecsI2Value(),
                SecsDataTypeEnum.I4Value => new SecsI4Value(),
                SecsDataTypeEnum.I8Value => new SecsI8Value(),

                SecsDataTypeEnum.U1Value => new SecsU1Value(),
                SecsDataTypeEnum.U2Value => new SecsU2Value(),
                SecsDataTypeEnum.U4Value => new SecsU4Value(),
                SecsDataTypeEnum.U8Value => new SecsU8Value(),

                SecsDataTypeEnum.F4Value => new SecsF4Value(),
                SecsDataTypeEnum.F8Value => new SecsF8Value(),

                _ => throw new IndexOutOfRangeException(),
            };
        }

        public static SecsDataTypeEnum GetSecsTreeNodeTypeByEnum(SecsTreeNode secsTreeNode)
        {
            return secsTreeNode switch
            {
                 SecsList => SecsDataTypeEnum.List,

                SecsAscii => SecsDataTypeEnum.ASCII,

                SecsBool => SecsDataTypeEnum.Boolean,

                SecsBinary => SecsDataTypeEnum.Binary,

                SecsI1 => SecsDataTypeEnum.I1,
                SecsI2 => SecsDataTypeEnum.I2,
                SecsI4 => SecsDataTypeEnum.I4,
                SecsI8 => SecsDataTypeEnum.I8,

                SecsU1 => SecsDataTypeEnum.U1,
                SecsU2 => SecsDataTypeEnum.U2,
                SecsU4 => SecsDataTypeEnum.U4,
                SecsU8 => SecsDataTypeEnum.U8,

                SecsF4 => SecsDataTypeEnum.F4,
                SecsF8 => SecsDataTypeEnum.F8,

                SecsBoolValue => SecsDataTypeEnum.BooleanValue,

                SecsBinaryValue => SecsDataTypeEnum.BinaryValue,

                SecsI1Value => SecsDataTypeEnum.I1Value,
                SecsI2Value => SecsDataTypeEnum.I2Value,
                SecsI4Value => SecsDataTypeEnum.I4Value,
                SecsI8Value => SecsDataTypeEnum.I8Value,

                SecsU1Value => SecsDataTypeEnum.U1Value,
                SecsU2Value => SecsDataTypeEnum.U2Value,
                SecsU4Value => SecsDataTypeEnum.U4Value,
                SecsU8Value => SecsDataTypeEnum.U8Value,

                SecsF4Value => SecsDataTypeEnum.F4Value,
                SecsF8Value => SecsDataTypeEnum.F8Value,

                _ => throw new IndexOutOfRangeException(),
            };
        }

        public static IEnumerable<SecsDataTypeWrapperClass> GetCompatibleChildrenType(SecsTreeNode? secsTreeNode)
        {
            if (secsTreeNode is null)
            {
                return GetSecsNodeDataTypeWrapperClasses();
            }
            var typeEnum = GetSecsTreeNodeTypeByEnum(secsTreeNode);
            switch (typeEnum)
            {
                case SecsDataTypeEnum.List:
                    return GetSecsNodeDataTypeWrapperClasses();
                case SecsDataTypeEnum.ASCII:
                    return Enumerable.Empty<SecsDataTypeWrapperClass>();
                case SecsDataTypeEnum.Boolean:
                case SecsDataTypeEnum.Binary:
                case SecsDataTypeEnum.I1:
                case SecsDataTypeEnum.I2:
                case SecsDataTypeEnum.I4:
                case SecsDataTypeEnum.I8:
                case SecsDataTypeEnum.U1:
                case SecsDataTypeEnum.U2:
                case SecsDataTypeEnum.U4:
                case SecsDataTypeEnum.U8:
                case SecsDataTypeEnum.F4:
                case SecsDataTypeEnum.F8:
                    return SecsDataTypeWrapperClasses().Where(x => x.Index == (int)typeEnum + 100);
                default:
                    return Enumerable.Empty<SecsDataTypeWrapperClass>();
            }
        }
    }

    public class COMMMODETypeWrapperClass : EnumWrapper
    {
        public COMMMODETypeWrapperClass(COMMMODE commmode)
        {
            Commmode = commmode;
            index = (int)commmode;
            displayName = commmode.ToString();
        }
        public COMMMODE Commmode { get; init; }
    }

    public class SECSCOMMMODETypeWrapperClass : EnumWrapper
    {
        public SECSCOMMMODETypeWrapperClass(SECS_COMM_MODE secsCommMode)
        {
            SECS_COMM_MODE = secsCommMode;
            index = (int)secsCommMode;
            displayName = secsCommMode.ToString();
        }
        public SECS_COMM_MODE SECS_COMM_MODE { get; init; }
    }

    public class HSMSCOMMMODETypeWrapperClass : EnumWrapper
    {
        public HSMSCOMMMODETypeWrapperClass(HSMS_COMM_MODE hsmsCommMode)
        {
            HSMS_COMM_MODE = hsmsCommMode;
            index = (int)hsmsCommMode;
            displayName = hsmsCommMode.ToString();
        }
        public HSMS_COMM_MODE HSMS_COMM_MODE { get; init; }
    }

    //children type +20
    public enum SecsDataTypeEnum
    {
        Unknown = 0,

        List = 1,

        ASCII = 2,

        Boolean = 3,

        Binary = 4,

        I1 = 5,
        I2 = 6,
        I4 = 7,
        I8 = 8,

        U1 = 9,
        U2 = 10,
        U4 = 11,
        U8 = 12,

        F4 = 13,
        F8 = 14,

        BooleanValue = 103,
        BinaryValue = 104,
        
        I1Value = 105,
        I2Value = 106,
        I4Value = 107,
        I8Value = 108,

        U1Value = 109,
        U2Value = 110,
        U4Value = 111,
        U8Value = 112,

        F4Value = 113,
        F8Value = 114,
    }

    public class SecsDataTypeWrapperClass : EnumWrapper
    {
        public SecsDataTypeWrapperClass(SecsDataTypeEnum secsDataTypeEnum)
        {
            SecsDataTypeEnum = secsDataTypeEnum;
            index = (int)secsDataTypeEnum;
            displayName = secsDataTypeEnum.ToString();
        }
        public SecsDataTypeEnum SecsDataTypeEnum { get; init; }
    }
}
