using QGACTIVEXLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class GemStatus
    {
        public event Func<Task>? GemUpdateFunc;
        private void GemUpdate()
        {
            if (GemUpdateFunc is null) return;

            foreach (var handler in GemUpdateFunc.GetInvocationList())
            {
                var func = (Func<Task>)handler;
                _ = Task.Run(func);
            }
        }
        public int UpdateSVDelay { get; set; } = 1000;
        public bool UpdateSV => UpdateSVDelay > 0;

        private bool initSuccess { get; set; }
        public bool InitSuccess => initSuccess;
        public void SetInitSuccess(bool initSuccess)
        {
            this.initSuccess = initSuccess;
            GemUpdate();
        }


        #region communicating
        private bool defaultCommunicating { get; set; }
        public bool DefaultCommunicating => defaultCommunicating;

        public void SetDefaultCommunicating(bool b)
        {
            defaultCommunicating = b;
            GemUpdate();
        }

        private COMM_STATE communicating { get; set; }
        public string CommunicatingString => communicating.ToString();

        public bool AllowCommunicating => communicating is not COMM_STATE.DISABLE;

        public bool IsCommunicating => communicating is COMM_STATE.COMMUNICATING;

        public void SetCommunicating(COMM_STATE communicating)
        {
            this.communicating = communicating;
            GemUpdate();
        }

        #endregion

        #region control
        private ControlState controlState { get; set; }
        
        public ControlState ControlState => controlState;

        public string ControlStateString => controlState.ToString();

        public void SetControlState(ControlState controlState)
        {
            this.controlState = controlState;
            GemUpdate();
        }

        private DefaultControlState defaultControlState { get; set; }
        public DefaultControlState DefaultControlState => defaultControlState;
        public string DefaultControlStateString => defaultControlState.ToString();
        public void SetDefaultControlState(DefaultControlState defaultControlState)
        {
            this.defaultControlState = defaultControlState;
            GemUpdate();
        }

        private DefaultOfflineOrOnlineFailSubstate defaultOfflineSubstate { get; set; }
        public DefaultOfflineOrOnlineFailSubstate DefaultOfflineSubstate => defaultOfflineSubstate;
        public string DefaultOfflineSubstateString => defaultOfflineSubstate.ToString();
        public void SetDefaultOfflineSubstate(DefaultOfflineOrOnlineFailSubstate defaultOfflineSubstate)
        {
            this.defaultOfflineSubstate = defaultOfflineSubstate;
            GemUpdate();
        }

        private DefaultOfflineOrOnlineFailSubstate defaultOnlineFailSubstate { get; set; }
        public DefaultOfflineOrOnlineFailSubstate DefaultOnlineFailSubstate => defaultOnlineFailSubstate;
        public string DefaultOnlineFailSubstateString => defaultOnlineFailSubstate.ToString();

        public void SetDefaultOnlineFailSubstate(DefaultOfflineOrOnlineFailSubstate defaultOnlineFailSubstate)
        {
            this.defaultOnlineFailSubstate = defaultOnlineFailSubstate;
            GemUpdate();
        }

        private DefaultOnlineSubstate defaultOnlineSubstate { get; set; }
        public DefaultOnlineSubstate DefaultOnlineSubstate => defaultOnlineSubstate;
        public string DefaultOnlineSubstateString => defaultOnlineSubstate.ToString();
        public void SetDefaultOnlineSubstate(DefaultOnlineSubstate defaultOnlineSubstate)
        {
            this.defaultOnlineSubstate = defaultOnlineSubstate;
            GemUpdate();
        }

        #endregion
    }
}
