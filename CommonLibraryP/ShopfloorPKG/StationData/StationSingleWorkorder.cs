using CommonLibraryP.API;
using CommonLibraryP.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.ShopfloorPKG
{
    public abstract class StationSingleWorkorder : Station
    {

        public StationSingleWorkorder(Station station)
        {
            Id = station.Id;
            ProcessId = station.ProcessId;
            Name = station.Name;
            ProcessIndex = station.ProcessIndex;
            StationType = station.StationType;
            Process = station.Process;
            Enable = station.Enable;
        }

        protected Workorder? workorder;
        [NotMapped]
        public Workorder? Workorder => workorder;

        public override int WorkorderAmount => workorder is null ? 0 : 1;

        public override bool CanDeployWorkorder => StationStatusCode is 4 && WorkorderAmount is 0;

        public override bool CanRun => StationStatusCode is 4 && WorkorderAmount is 1;

        

        public override RequestResult SetWorkorder(Workorder wo)
        {
            if (WorkorderAmount is 1)
            {
                return new(4, "Workorder already exist");
            }
            if (!CanDeployWorkorder)
            {
                return new(4, "Station is not at idle status");
            }
            workorder = wo;
            UIUpdate();
            return new(2, $"Station {Name} set workorder {wo.WorkorderNo}-{wo.Lot} success");
        }

        public override RequestResult Run()
        {
            if (WorkorderAmount is not 1)
            {
                return new(4, $"Station {Name} workorder amount invalid{WorkorderAmount}");
            }
            if (!CanRun)
            {
                return new(4, "Station is not at idle status");
            }
            SetStationStatus(5);
            UIUpdate();
            return new(2, $"Run station {Name} success");
        }

        public override RequestResult ClearWorkorder()
        {
            if (!CanClear)
            {
                return new(4, $"Station {Name} not allow to clear");
            }
            else
            {
                workorder = null;
                SetStationStatus(4);
                UIUpdate();
                return new(2, $"Station {Name} clear workorder success");
            }
        }




        //public virtual RequestResult RemoveItemDetail() => throw new NotImplementedException();

        //public virtual RequestResult RemoveTaskDetail() => throw new NotImplementedException();
    }
}
