using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Templates;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.LogPKG
{
    public class SerilogService
    {
        private string connectionString;

        private readonly ConcurrentDictionary<string, ILogger> loggerCache = new();

        private readonly ColumnOptions columnOptions; 
        public SerilogService(string connectionString)
        {
            this.connectionString = connectionString;
            columnOptions = new ColumnOptions();

            columnOptions.Store.Remove(StandardColumn.MessageTemplate);
            columnOptions.Store.Remove(StandardColumn.Level);
            columnOptions.Store.Remove(StandardColumn.Exception);
            columnOptions.Store.Remove(StandardColumn.Properties);
            columnOptions.AdditionalColumns = new Collection<SqlColumn>
            {
                new SqlColumn { ColumnName = "LogLevel", DataType = SqlDbType.Int },
                new SqlColumn { ColumnName = "CallerNameSpace", DataType = SqlDbType.NVarChar, DataLength = 128 },
                new SqlColumn { ColumnName = "ClassName", DataType = SqlDbType.NVarChar, DataLength = 128 },
                new SqlColumn { ColumnName = "MethodName", DataType = SqlDbType.NVarChar, DataLength = 128 },
                new SqlColumn { ColumnName = "LineNumber", DataType = SqlDbType.Int }

            };
        }

        private string GetCallerNamespace(char seperator)
        {
            var callerType = new StackTrace().GetFrame(3)?.GetMethod()?.DeclaringType;
            var namespaceName = callerType?.Namespace;

            return string.IsNullOrEmpty(namespaceName) ? "Anonymous" : namespaceName.Replace('.', seperator);
        }

        #region console
        private void ConsoleLogger(LogEventLevel logEventLevel, string msg)
        {
            var logger = loggerCache.GetOrAdd("consoleLoggerConf", x =>
            {
                return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Async(
                    x => x.Console(theme: SystemConsoleTheme.Colored))
                .CreateLogger();
            });
            logger.Write(logEventLevel, msg);
        }
        public void ConsoleVerbose(string msg)
        {
            ConsoleLogger(LogEventLevel.Verbose, msg);
        }
        public void ConsoleDebug(string msg)
        {
            ConsoleLogger(LogEventLevel.Debug, msg);
        }
        public void ConsoleInformation(string msg)
        {
            ConsoleLogger(LogEventLevel.Information, msg);
        }
        public void ConsoleWarning(string msg)
        {
            ConsoleLogger(LogEventLevel.Warning, msg);
        }
        public void ConsoleError(string msg)
        {
            ConsoleLogger(LogEventLevel.Error, msg);
        }
        public void ConsoleFatal(string msg)
        {
            ConsoleLogger(LogEventLevel.Fatal, msg);
        }
        #endregion

        #region text file
        private void TextLogger(LogEventLevel logEventLevel, string msg ,string className = "", string methodName = "", int lineNumber = 0)
        {
            var callerns = GetCallerNamespace('/');
            var logger = loggerCache.GetOrAdd(className, x =>
            {
                return new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Async(
                        x => x.File(
                        path: $"logs/{callerns}_.txt",
                        rollingInterval: RollingInterval.Day,
                        formatter: new ExpressionTemplate("{ {Timestamp: @t, Level: @l, Message: @m,CallerNameSpace ,ClassName, MethodName, LineNumber, LogLevel} }\n"),
                        //outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
                        retainedFileCountLimit: 31
                    ))
                    .CreateLogger();
            });
            logger
                .ForContext("LogLevel", (int)logEventLevel)
                .ForContext("CallerNameSpace", callerns)
                .ForContext("ClassName", className)
                .ForContext("MethodName", methodName)
                .ForContext("LineNumber", lineNumber)
                .Write(logEventLevel, msg);
        }

        public void TextVerbose(string msg,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            var callerType = new StackTrace().GetFrame(1)?.GetMethod()?.DeclaringType;
            var namespaceName = callerType?.Namespace;


            TextLogger(LogEventLevel.Verbose, msg, methodName: methodName, className: Path.GetFileNameWithoutExtension(sourceFilePath), lineNumber: lineNumber);
        }
        public void TextDebug(string msg,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            TextLogger(LogEventLevel.Debug, msg, methodName: methodName, className: Path.GetFileNameWithoutExtension(sourceFilePath), lineNumber: lineNumber);
        }
        public void TextInformation(string msg,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            TextLogger(LogEventLevel.Information, msg, methodName: methodName, className: Path.GetFileNameWithoutExtension(sourceFilePath), lineNumber: lineNumber);
        }
        public void TextWarning(string msg,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            TextLogger(LogEventLevel.Warning, msg, methodName: methodName, className: Path.GetFileNameWithoutExtension(sourceFilePath), lineNumber: lineNumber);
        }
        public void TextError(string msg,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            TextLogger(LogEventLevel.Error, msg, methodName: methodName, className: Path.GetFileNameWithoutExtension(sourceFilePath), lineNumber: lineNumber);
        }
        public void TextFatal(string msg,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            TextLogger(LogEventLevel.Fatal, msg, methodName: methodName, className: Path.GetFileNameWithoutExtension(sourceFilePath), lineNumber: lineNumber);
        }


        #endregion

        #region db

        private void DbLogger(LogEventLevel logEventLevel, string msg, string className = "", string methodName = "", int lineNumber = 0)
        {
            var callerns = GetCallerNamespace('-');
            var logger = loggerCache.GetOrAdd("dbLoggerConf", x =>
            {
                return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Async(
                    x => x.MSSqlServer(
                        connectionString: connectionString,
                        sinkOptions: new MSSqlServerSinkOptions
                        {
                            TableName = "SerilogMSSQLLogs",
                            AutoCreateSqlTable = true,
                        },
                        columnOptions: columnOptions))
                .CreateLogger();
            });
            logger
                .ForContext("LogLevel", (int)logEventLevel)
                .ForContext("CallerNameSpace", callerns)
                .ForContext("ClassName", className)
                .ForContext("MethodName", methodName)
                .ForContext("LineNumber", lineNumber)
                .Write(logEventLevel, msg);
        }
        public void DbVerbose(string msg,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {

            DbLogger(LogEventLevel.Verbose, msg, methodName: methodName, className: Path.GetFileNameWithoutExtension(sourceFilePath), lineNumber: lineNumber);
        }
        public void DbDebug(string msg,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            DbLogger(LogEventLevel.Debug, msg, methodName: methodName, className: Path.GetFileNameWithoutExtension(sourceFilePath), lineNumber: lineNumber);
        }
        public void DbInformation(string msg,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            DbLogger(LogEventLevel.Information, msg, methodName: methodName, className: Path.GetFileNameWithoutExtension(sourceFilePath), lineNumber: lineNumber);
        }
        public void DbWarning(string msg,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            DbLogger(LogEventLevel.Warning, msg, methodName: methodName, className: Path.GetFileNameWithoutExtension(sourceFilePath), lineNumber: lineNumber);
        }
        public void DbError(string msg,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            DbLogger(LogEventLevel.Error, msg, methodName: methodName, className: Path.GetFileNameWithoutExtension(sourceFilePath), lineNumber: lineNumber);
        }
        public void DbeFatal(string msg,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            DbLogger(LogEventLevel.Fatal, msg, methodName: methodName, className: Path.GetFileNameWithoutExtension(sourceFilePath), lineNumber: lineNumber);
        }

        #endregion
    }
}
