using QSACTIVEXLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SECSStatus
    {
        public event Func<Task>? SECSUpdateFunc;
        private void SECSUpdate()
        {
            if (SECSUpdateFunc is null) return;

            foreach (var handler in SECSUpdateFunc.GetInvocationList())
            {
                var func = (Func<Task>)handler;
                _ = Task.Run(func);
            }
        }

        //hsms
        private bool hosting { get; set; }
        public bool Hosting => hosting;
        public void SetHosting(bool hosting)
        {
            this.hosting = hosting;
            SECSUpdate();
        }

        private bool connected { get; set; }
        public bool Connected => connected;
        public void SetConnected(bool connected)
        {
            this.connected = connected;
            SECSUpdate();
        }


    }
}
