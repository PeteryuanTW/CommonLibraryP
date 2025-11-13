using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SECSGemStatus
    {
        private bool hosting { get; set; }
        public bool Hosting => hosting;

        private bool connected { get; set; }
        public bool Connected => connected;

        private bool communicating { get; set; }
        public bool Communicating => communicating;


        public void SetHosting(bool hosting)
        {
            this.hosting = hosting;
        }

        public void SetConnected(bool connected)
        {
            this.connected = connected;
        }

        public void SetCommunicating(bool communicating)
        {
            this.communicating = communicating;
        }
    }
}
