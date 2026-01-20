using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SendEventParameter
    {
        [Range(100, int.MaxValue)]
        public int EventId { get; set; }
    }
}
