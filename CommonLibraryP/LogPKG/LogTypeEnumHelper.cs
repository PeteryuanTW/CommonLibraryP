using CommonLibraryP.Data;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.LogPKG
{
    public static class LogTypeEnumHelper
    {
        public static IEnumerable<LogLevelTypeWrapperClass> GetLogLevelsWrapperClass()
        {
            return Enum.GetValues(typeof(LogEventLevel)).OfType<LogEventLevel>()
                .Select(x => new LogLevelTypeWrapperClass(x));
        }
    }

    public class LogLevelTypeWrapperClass : EnumWrapper
    {
        public LogLevelTypeWrapperClass(LogEventLevel logEventLevel)
        {
            LogEventLevel = logEventLevel;
            index = (int)logEventLevel;
            displayName = logEventLevel.ToString();
        }

        

        public LogEventLevel LogEventLevel { get; init; }
    }
}
