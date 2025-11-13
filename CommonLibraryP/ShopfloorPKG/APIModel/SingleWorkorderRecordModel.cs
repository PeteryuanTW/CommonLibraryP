using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.ShopfloorPKG
{
    public class SingleWorkorderRecordModel
    {
        [Required]
        public string serialNo { get; set; } = null!;
        [Required]
        public string recordName { get; set; } = null!;
        [Required]
        public string recordValue { get; set; } = null!;
    }
}
