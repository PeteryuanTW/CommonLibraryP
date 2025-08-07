using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public class TagWarningBoolCondition : TagWarningCondition
    {
        public bool TargetBoolValue { get; set; }
        public override string TargetValueString => TargetBoolValue.ToString();
    }
}
