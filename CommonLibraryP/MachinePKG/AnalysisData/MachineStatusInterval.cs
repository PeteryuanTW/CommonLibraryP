using CommonLibraryP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public class MachineStatusInterval
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public int StatusCode { get; set; }

        public TimeSpan Interval => End - Start;

        public DateTime MidTime => Start + Interval;

        public MachineStatusInterval(DateTime start, DateTime end, int statusCode)
        {
            Start = start;
            End = end;
            StatusCode = statusCode;
        }
    }
}
