using CommonLibraryP.API;
using CommonLibraryP.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommonLibraryP.ShopfloorPKG
{

    public partial class Station
    {
        public Station() { }

        public Station(Guid processId)
        {
            ProcessId = processId;
        }

        public bool IsSingleWorkorder => ((StationType / 10) % 10) switch
        {
            1 => true,
            2 => false,
            _ => throw new IndexOutOfRangeException($"IsSingleWorkorder flag error(value{(StationType / 100) % 10})"),
        };

        public bool IsSingleItem => (StationType% 10) switch
        {
            1 => true,
            2 => false,
            3 => false,
            _ => throw new IndexOutOfRangeException($"IsSingleItem flag error(value{(StationType / 10) % 10})"),
        };

        public bool WithSerialNo => (StationType % 10) is not 3;


        private int stationStatusCode = 0;
        public int StationStatusCode => stationStatusCode;
        public bool IsRunning => stationStatusCode is 5;

        protected void SetStationStatus(int statusCode, string msg = "Normal")
        {
            stationStatusCode = statusCode;
            SetErrorMsg(msg);
            UIUpdate();
        }

        private string errorMsg = String.Empty;
        public string ErrorMsg => errorMsg;

        private void SetErrorMsg(string s)
        {
            errorMsg = stationStatusCode is 8 ? s : "Normal";
        }
        protected void UIUpdate()
        {
            UIUpdateAct?.Invoke();
        }
        public Func<Task>? UIUpdateAct;


        public void InitStation()
        {
            stationStatusCode = 4;
            errorMsg = String.Empty;
            UIUpdate();
        }

        #region workorder
        
        public virtual int WorkorderAmount => throw new NotImplementedException();
        public virtual bool CanDeployWorkorder => throw new NotImplementedException();
        public virtual bool CanRun => throw new NotImplementedException();
        //public virtual bool CanStop => throw new NotImplementedException();
        public virtual bool CanClear => throw new NotImplementedException();
        #endregion

        #region workorder operation
        public virtual RequestResult SetWorkorder(Workorder wo)
        {
            return new(4, "not implement yet");
        }
        public virtual RequestResult ClearWorkorder()
        {
            return new(4, "not implement yet");
        }

        #endregion

        #region item
        public virtual int WIPItemAmount => throw new NotImplementedException();
        #endregion

        #region item operation
        public virtual RequestResult CheckCanAddItem()
        {
            return IsRunning ? new RequestResult(2, $"Station {Name} add item when running") : new RequestResult(4, $"Station {Name} is not running");
        }
        public bool CanStationIn => CheckCanAddItem().IsSuccess;
        public virtual RequestResult AddItemDetail(ItemDetail itemDetail) => throw new NotImplementedException();
        
        public virtual bool CheckItemIsWIP(string serialNo) => throw new NotImplementedException();

        public virtual RequestResult RefreshItemAndRecord(ItemDetail itemDetail) => throw new NotImplementedException();

        public virtual RequestResult CheckCanRemoveItem()
        {
            return IsRunning ? new RequestResult(2, $"Station {Name} add item when running") : new RequestResult(4, $"Station {Name} is not running") ;
        }
        public bool CanStationOut => CheckCanRemoveItem().IsSuccess;
        
        #endregion

        #region task
        protected virtual int WIPTaskAmount => throw new NotImplementedException();
        //public virtual bool TaskAmountValid => throw new NotImplementedException();
        #endregion

        #region task operation

        public virtual RequestResult CheckCanAddTask()
        {
            return IsRunning ? new RequestResult(2, $"Station {Name} add task when running") : new RequestResult(4, $"Station {Name} is not running");
        }
        public virtual RequestResult AddTaskDetail(TaskDetail taskDetail) => throw new NotImplementedException();

        public virtual RequestResult CheckCanRemoveTask()
        {
            return IsRunning ? new RequestResult(2, $"Station {Name} remove task when running") : new RequestResult(4, $"Station {Name} is not running");

        }

        #endregion

        #region station status
        public virtual RequestResult Run()
        {
            return new(4, "not implement yet");
        }

        public virtual RequestResult Pause()
        {
            return new(4, "not implement yet");
        }

        public virtual RequestResult Stop()
        {
            return new(4, "not implement yet");
        }

        protected void Error(string s)
        {
            stationStatusCode = 8;
            SetErrorMsg(s);
            UIUpdate();
        }
        #endregion
    }
}
