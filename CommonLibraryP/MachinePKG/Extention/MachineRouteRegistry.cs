using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public class MachineRouteRegistry
    {
        private static readonly Dictionary<string, Type> _routes = new();

        public static void RegisterRoute(string route, Type componentType)
        {
            _routes[route] = componentType;
        }

        public static IReadOnlyDictionary<string, Type> Routes => _routes;


    }
}
