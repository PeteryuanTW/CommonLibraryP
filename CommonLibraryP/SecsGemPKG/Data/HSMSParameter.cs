using QSACTIVEXLib;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class HSMSParameter : SECSCommonParameter
    {
		[Required]
		public int T5 { get; set; } = 10;
		[Required]
		public int T6 { get; set; } = 5;
		[Required]
		public int T7 { get; set; } = 10;
		[Required]
		public int T8 { get; set; } = 5;
		[Required]
		public int LinkTestPeriod { get; set; } = 60;
		[Required]
		[RegularExpression(@"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)")]
		public string LocalIP { get; set; } = "127.0.0.1";
		[Required]
		[Range(0, 65535)]
		public int LocalPort { get; set; } = 5000;
		[Required]
		[RegularExpression(@"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)")]
		public string RemoteIP { get; set; } = "127.0.0.1";
		[Required]
		[Range(0, 65535)]
		public int RemotePort { get; set; } = 5000;
		[Required]
		public HSMS_COMM_MODE HSMS_Connect_Mode { get; set; }
    }
}
