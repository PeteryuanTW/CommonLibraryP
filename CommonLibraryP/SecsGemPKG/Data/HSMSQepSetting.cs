using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class HSMSQepSetting
    {
        public int T3 { get; set; }
        public int DeviceID { get; set; }
        public int T5 { get; set; } = 10;
        public int T6 { get; set; } = 5;
        public int T7 { get; set; } = 10;
        public int T8 { get; set; } = 5;
        public int LinkTestPeriod { get; set; } = 60;
        public string LocalIP { get; set; } = "127.0.0.1";
        public int LocalPort { get; set; } = 5000;
        public string RemoteIP { get; set; } = "127.0.0.1";
        public int RemotePort { get; set; } = 5000;
    }
}
