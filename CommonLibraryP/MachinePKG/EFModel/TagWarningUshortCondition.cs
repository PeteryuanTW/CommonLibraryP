using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public class TagWarningUshortCondition :TagWarningCondition
    {
        public int TargetUshortValue { get; set; }

        public override string TargetValueString => TargetUshortValue.ToString();
    }
}
