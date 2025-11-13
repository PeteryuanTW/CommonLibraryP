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

    public enum SecsDataTypeEnum
    {
        Unknown = 0,
        List = 1,
        Binary = 2,
        Boolean = 3,
        ASCII = 4,
        I8 = 5,
        I1 = 6,
        I2 = 7,
        I4 = 8,
        F8 = 9,
        F4 = 10,
        U8 = 11,
        U1 = 12,
        U2 = 13,
        U4 = 14
    }
}
