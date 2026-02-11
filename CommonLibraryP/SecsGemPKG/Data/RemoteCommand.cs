using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class RemoteCommand
    {
        public string Name { get; set; }

        public Dictionary<string, string> ParameterList;

        public RemoteCommand()
        {
			Name = string.Empty;
			ParameterList = new Dictionary<string, string>();
		}

		public RemoteCommand(string name)
        {
			Name = name;
			ParameterList = new Dictionary<string, string>();
		}
	}
}
