using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MapPKG
{
    public  abstract partial class MapComponent
    {
        public Guid Id { get; set; }
        public Guid MapId { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public virtual MapConfig? MapConfig { get; set; }
    }
}
