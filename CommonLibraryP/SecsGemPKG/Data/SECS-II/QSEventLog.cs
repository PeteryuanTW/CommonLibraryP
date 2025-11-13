using QSACTIVEXLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class QSEventLog
    {
        public EVENT_ID EventType { get; set; }
        public int S { get; set; }
        public int F { get; set; }
        public DateTime LogTime { get; set; }
        public SecsTreeNode? SecsItem { get; set; }
    }
}
