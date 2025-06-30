using CommonLibraryP.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Blazor;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text.RegularExpressions;

namespace CommonLibraryP.MachinePKG
{
    public static class MachineTypeEnumHelper
    {
        #region connection type
        public static List<ConnectionTypeWrapperClass> GetConnectionTypeWrapperClasses() => connectionTypeWrapperClasses;
        private static List<ConnectionTypeWrapperClass> connectionTypeWrapperClasses = new()
        {
            new(0, typeof(ModbusTCPMachine)),
            new(1, typeof(TMRobotModbusTCP)),
        };

        public static List<ConnectionTypeWrapperClass> GetSuitableConnectionTypeWrapperClasses(int currentIndex)
        {
            var target = GetConnectionTypeWrapperClassByIndex(currentIndex);
            //foreach (var a in connectionTypeWrapperClasses)
            //{
            //    var x = a.Type.GetType();
            //    var r = x.IsAssignableFrom(target?.Type);
            //}
            return connectionTypeWrapperClasses.Where(x => x.Type.IsAssignableFrom(target?.Type)).ToList();
        }
        
        public static ConnectionTypeWrapperClass? GetConnectionTypeWrapperClassByIndex(int index)
        {
            return connectionTypeWrapperClasses.FirstOrDefault(x => x.Index == index);
        }

        public static void AddCustomConnection<T>(int index) where T : Machine
        {
            if (!connectionTypeWrapperClasses.Any(x => x.Index == index))
            {
                connectionTypeWrapperClasses.Add(new(index, typeof(T)));
            }
        }

        //public static IEnumerable<ConnectionTypeWrapperClass> GetConnectTypesWrapperClass()
        //{
        //    return Enum.GetValues(typeof(ConnectType)).OfType<ConnectType>()
        //        .Select(x => new ConnectionTypeWrapperClass(x));
        //}
        #endregion

        #region data type
        public static IEnumerable<DataTypeWrapperClass> GetDataTypesWrapperClass()
        {
            return Enum.GetValues(typeof(DataType)).OfType<DataType>()
                .Select(x => new DataTypeWrapperClass(x));
        }
        public static Type GetTypeByCode(int code)
        {
            var target = GetDataTypesWrapperClass().FirstOrDefault(x => x.Index == code);
            return target is null ? typeof(Object) : target.csType;
        }

        public static Dictionary<DataType, Type> TypeDict = new()
        {
            { DataType.Bool, typeof(bool) },
            { DataType.Ushort, typeof(ushort) },
            //{ DataType.Float, typeof(float) },
            { DataType.String, typeof(string) },
            { DataType.ArrayOfBool, typeof(bool[]) },
            { DataType.ArrayOfUshort, typeof(ushort[]) },
            //{ DataType.ArrayOfFloat, typeof(float[]) },
            //{ DataType.ArrayOfString, typeof(string[]) },

        };
        public static bool TypeMatch(int? code, Type? type)
        {
            if (code is null || type is null)
            {
                return false;
            }
            DataType dt = (DataType)code;
            if (TypeDict.ContainsKey(dt))
            {
                if (TypeDict[dt] == type)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool TypeMatch(DataType dt, Type type)
        {
            if (TypeDict.ContainsKey(dt))
            {
                if (TypeDict[dt] == type)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool VerifyValueStringWithDatatype(int datatype, string valString)
        {
            try
            {
                return Regex.IsMatch(valString, GetStringPatternByDatatype(datatype), RegexOptions.IgnoreCase);

            }
            catch
            {
                return false;
            }

        }

        private static string GetStringPatternByDatatype(int datatype)
        {
            switch (datatype)
            {
                case 1:
                    return @"^(true|false)$"; // boolean
                case 2:
                    return @"^\d+$"; // ushort
                case 4:
                    return string.Empty; // string
                case 11:
                    return @"^\[(true|false)(,(true|false))*\]$"; // boolean[]
                case 22:
                    return @"^\[(\d{1,5})(,(\d{1,5}))*\]$"; // ushort[]
                default:
                    throw new ArgumentException($"Unsupported datatype: {datatype}");

            }

        }
        #endregion
        #region special tag type
        //public static IEnumerable<SpecialTagTypeWrapperClass> GetSpecialTagTypesWrapperClass()
        //{
        //    return Enum.GetValues(typeof(SpecialTagType)).OfType<SpecialTagType>()
        //        .Select(x => new SpecialTagTypeWrapperClass(x));
        //}
        #endregion
        #region tag parameter
        //private static List<TagParameter> TagParameterDict = new()
        //{
        //    //modbus tcp
        //    new TagParameter( ConnectType.ModbusTCP, "Bool1", "Input/Output" ),
        //    new TagParameter( ConnectType.ModbusTCP, "Bool2", "String Reverse" ),

        //    new TagParameter( ConnectType.ModbusTCP, "Int1", "Station No" ),
        //    new TagParameter( ConnectType.ModbusTCP, "Int2", "Start Index" ),
        //    new TagParameter( ConnectType.ModbusTCP, "Int3", "Offset" ),

        //    //tm robot
        //    new TagParameter( ConnectType.TMRobot, "Bool1", "Input/Output" ),
        //    new TagParameter( ConnectType.TMRobot, "Bool2", "String Reverse" ),

        //    new TagParameter( ConnectType.TMRobot, "Int1", "Station No" ),
        //    new TagParameter( ConnectType.TMRobot, "Int2", "Start Index" ),
        //    new TagParameter( ConnectType.TMRobot, "Int3", "Offset"),

        //    //conveyor
        //    //new TagParameter( ConnectType.ConveyorMachine, "Bool1", "Input/Output" ),
        //    //new TagParameter( ConnectType.ConveyorMachine, "Bool2", "String Reverse" ),

        //    //new TagParameter( ConnectType.ConveyorMachine, "Int1", "Station No" ),
        //    //new TagParameter( ConnectType.ConveyorMachine, "Int2", "Start Index" ),
        //    //new TagParameter( ConnectType.ConveyorMachine, "Int3", "Offset" ),

        //    //Wrapping
        //    //new TagParameter( ConnectType.WrappingMachine, "Bool1", "Input/Output" ),
        //    //new TagParameter( ConnectType.WrappingMachine, "Bool2", "String Reverse" ),

        //    //new TagParameter( ConnectType.WrappingMachine, "Int1", "Station No" ),
        //    //new TagParameter( ConnectType.WrappingMachine, "Int2", "Start Index" ),
        //    //new TagParameter( ConnectType.WrappingMachine, "Int3", "Offset" ),


        //    //Web api
        //    new TagParameter( ConnectType.WebAPI, "String1", "Get Controller" ),
        //    new TagParameter( ConnectType.WebAPI, "String2", "Post COntroller" ),

        //    //rfid
        //    //new TagParameter( ConnectType.RegalscanRFID, "String1", "Get Controller" ),
        //    //new TagParameter( ConnectType.RegalscanRFID, "String2", "Post COntroller" ),
        //};

        //public static string GetTagParameterMeaning(ConnectType connectType, string varName)
        //{
        //    var target = TagParameterDict.FirstOrDefault(x => x.connectType == connectType && x.variableName == varName);
        //    return target is null ? "Not Defined" : target.parameterName;
        //}
        #endregion

        #region logical operations

        public static List<LogicalOperationWrapperClass> LogicalOperationWrapperClassDict = new()
        {
            new LogicalOperationWrapperClass(LogicalOperation.Equal, "=="),
            new LogicalOperationWrapperClass(LogicalOperation.NotEqual, "!="),
            //new LogicalOperationWrapperClass(LogicalOperation.Large, ">"),
            //new LogicalOperationWrapperClass(LogicalOperation.Less, "<"),
            //new LogicalOperationWrapperClass(LogicalOperation.LargerThanOrEqualTo, ">="),
            //new LogicalOperationWrapperClass(LogicalOperation.LessThanOrEqualTo, "<="),

        };

        public static string GetLogicalOperationSymbol(int code)
        {
            var target = LogicalOperationWrapperClassDict.FirstOrDefault(x => x.Index == code);
            return target is null ? "?" : target.Symbol;
        }

        public static List<ConditionCommandCodeWrapperClass> ConditionCommandCodeWrapperClassDict = new()
        {
            new ConditionCommandCodeWrapperClass(ConditionCommandCode.Await),
            new ConditionCommandCodeWrapperClass(ConditionCommandCode.SetTagValue),

        };

        #endregion
    }

    #region connection type
    public class ConnectionTypeWrapperClass : EnumWrapper
    {
        public ConnectionTypeWrapperClass(int index, Type type)
        {
            this.index = index;
            this.type = type;
            displayName = type.Name.Split(".").LastOrDefault();
        }
        //public ConnectType Type { get; init; }
        //public int ParentIndex { get; init; }
        private Type type;
        public Type Type => type;
    }
    #endregion

    #region status type
    public enum MachineStatusRecordType
    {
        InputStatus = 0,
        CustomStatus = 1,
    }
    #endregion

    #region data type
    public class DataTypeWrapperClass : EnumWrapper
    {
        public DataTypeWrapperClass(DataType dataType)
        {
            Type = dataType;
            index = (int)Type;
            displayName = Type.ToString();
        }

        public DataType Type { get; init; }
        public Type csType => MachineTypeEnumHelper.TypeDict[Type];
    }
    public enum DataType
    {
        Bool = 1,
        Ushort = 2,
        //Float = 3,
        String = 4,
        ArrayOfBool = 11,
        ArrayOfUshort = 22,
        //ArrayOfFloat = 33,
        //ArrayOfString = 44,
    }
    #endregion

    #region editmode
    public enum DataEditMode
    {
        Insert,
        Update,
        Delete,
    }
    #endregion

    #region special tag type

    //public class SpecialTagTypeWrapperClass : EnumWrapper
    //{
    //    public SpecialTagTypeWrapperClass(SpecialTagType specialTagType)
    //    {
    //        index = (int)specialTagType;
    //        displayName = specialTagType.ToString();
    //    }
    //}
    //public enum SpecialTagType
    //{
    //    General,
    //    CustomStatus,
    //    DetailCode,
    //}
    #endregion

    #region logical operations

    public enum LogicalOperation
    {
        Equal = 1,
        NotEqual = 2,
        Large = 3,
        Less = 4,
        LargerThanOrEqualTo = 5,
        LessThanOrEqualTo = 6,
    }

    public class LogicalOperationWrapperClass : EnumWrapper
    {
        private string symbol;
        public string Symbol => symbol;
        public LogicalOperationWrapperClass(LogicalOperation logicalOperation, string symbol)
        {
            index = (int)logicalOperation;
            displayName = logicalOperation.ToString();
            this.symbol = symbol;
        }
    }

    public enum ConditionCommandCode
    {
        Await = 0,
        SetTagValue = 1,
    }

    public class ConditionCommandCodeWrapperClass : EnumWrapper
    {
        public ConditionCommandCodeWrapperClass(ConditionCommandCode conditionCommandCode)
        {
            index = (int)conditionCommandCode;
            displayName = conditionCommandCode.ToString();
        }
    }

    #endregion
}
