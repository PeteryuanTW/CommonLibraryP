using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MapPKG
{
    public class MapComponentMachine : MapComponent
    {
        [Required(ErrorMessage = "Machine is required")]
        public Guid MachineId { get; set; }
    }
}
