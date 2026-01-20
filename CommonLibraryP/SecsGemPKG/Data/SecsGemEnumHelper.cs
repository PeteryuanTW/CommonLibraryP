using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.SecsGemPKG
{
    public class SecsGemEnumHelper
    {
    }

    public enum ControlState
    {
        OffLine_EQPOffline = 1,
        OffLine_AttmptOnLine = 2,
        OffLine_HostOffline = 3,
        OnLine_Local = 4,
        OnLine_Remote = 5,
    }

    public enum DefaultControlState
    {
        OffLine = 1,
        OnLine = 2,
    }

    public enum DefaultOfflineOrOnlineFailSubstate
    {
        EqpOffLine = 1,
        AttemptOnLine = 2,
        HostOffLine = 3,
    }

    public enum DefaultOnlineSubstate
    {
        Local = 4,
        Remote = 5,
    }

}
