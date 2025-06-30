using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.ShopfloorPKG
{
    public class SingleSarialNoStationInModel
    {
        [Required]
        public string StationName { get; set; } = string.Empty;
        [Required]
        public string SerialNo { get; set; } = string.Empty;
    }
}
