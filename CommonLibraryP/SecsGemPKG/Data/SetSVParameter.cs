using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SetSVParameter
    {
        [Required]
        public string Name { get; set; } = null!;
		[Required]
		public string ValueString { get; set; } = null!;
	}
}
