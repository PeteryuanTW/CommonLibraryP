using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public partial class TagWarningCondition
    {
        private bool isWarning { get; set; }
        public bool IsWarning => isWarning;

        private DateTime? warningTime { get; set; }
        public DateTime? WarningTime => warningTime;

        public void TriggerWarning()
        {
            if (!isWarning)
            {
                isWarning = true;
                warningTime = DateTime.Now;
            }

        }

        public void DismissWarning()
        {
            if (isWarning)
            {
                isWarning = false;
                warningTime = null;
            }

        }
    }
}
