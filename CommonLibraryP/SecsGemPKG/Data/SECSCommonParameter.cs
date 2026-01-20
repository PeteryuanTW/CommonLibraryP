using QSACTIVEXLib;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SECSCommonParameter
    {
        public int Id { get; set; } = 1;
		[Required]
		public int T3 { get; set; } = 45;
		[Required]
		public int DeviceID { get; set; } = 0;
		[Required]
		public COMMMODE CommMode { get; set; }
    }
}
