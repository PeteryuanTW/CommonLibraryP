using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MapPKG
{
    public abstract partial class MapComponent : IEquatable<MapComponent>
    {
        public bool Equals(MapComponent? other)
        {
            return other?.Id == Id;
        }
    }
}
