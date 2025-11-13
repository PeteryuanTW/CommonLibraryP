using CommonLibraryP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.ShopfloorPKG
{
    public partial class Workorder
    {
        public string WorkorderNoAndLot => $"{WorkorderNo}-{Lot}";

        public string StatusString => CommonEnumHelper.GetStatusDetail(Status).DisplayName;

        public bool CanRun => Status is 0;
        public bool CanStop => Status is 5;

        public void Start()
        {
            StartTime = DateTime.Now;
            Status = 5;
        }

        public void Stop()
        {
            FinishedTime = DateTime.Now;
            Status = 7;
        }
    }
}
