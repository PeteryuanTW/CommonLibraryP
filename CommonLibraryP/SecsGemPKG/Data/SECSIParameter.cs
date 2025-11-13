using QSACTIVEXLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SECSIParameter : SECSCommonParameter
    {
        public float T1 { get; set; }
        public float T2 { get; set; }
        public int T4 { get; set; }
        public int BaudRate { get; set; }
        public int COMPort { get; set; }
        public int RTY { get; set; }
        public SECS_COMM_MODE SECS_Connect_Mode { get; set; }
    }
}
