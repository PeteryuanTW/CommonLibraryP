using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public class MachineRouteAttribute : Attribute
    {
        public string Route { get; }

        public MachineRouteAttribute(string route)
        {
            Route = route;
        }


    }
}
