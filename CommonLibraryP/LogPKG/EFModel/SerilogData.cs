using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.LogPKG
{
    public class SerilogData
    {
        public DateTime Timestamp { get; set; }
        public int LogLevel { get; set; }
        public string Message { get; set; }
        public string CallerNameSpace { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public int LineNumber { get; set; }

    }
}
