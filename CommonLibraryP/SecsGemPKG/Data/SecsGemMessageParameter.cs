using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SecsGemMessageParameter
    {
        [Range(0, 100)]
        public int S { get; set; }
        [Range(0, 100)]
        public int F { get; set; }
        [Required]
        public Guid RootId { get; set; }
    }
}
