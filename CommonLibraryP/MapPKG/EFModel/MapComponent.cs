using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MapPKG
{
    public abstract partial class MapComponent
    {
        public Guid Id { get; set; }
        public Guid MapId { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public double Height { get; set; } = 0.2f;
        public double Width { get; set; } = 0.2f;
        public virtual MapConfig? MapConfig { get; set; }
    }
}
