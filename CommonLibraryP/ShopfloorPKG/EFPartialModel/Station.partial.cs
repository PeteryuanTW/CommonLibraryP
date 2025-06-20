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

        public bool IsSingleWorkorder => ((StationType / 100) % 10) switch
        {
            1 => true,
            2 => false,
            _ => throw new IndexOutOfRangeException($"IsSingleWorkorder flag error(value{(StationType / 100) % 10})"),
        };

        public bool IsSingleItem => ((StationType / 10) % 10) switch
        {
            1 => true,
            2 => false,
            _ => throw new IndexOutOfRangeException($"IsSingleItem flag error(value{(StationType / 10) % 10})"),
        };

        public bool WithSerialNo => (StationType % 10) switch
        {
            1 => true,
            2 => false,
            _ => throw new IndexOutOfRangeException($"WithSerialNo flag error(value{StationType % 10})"),
        };


        private int stationStatusCode = 0;
        public int StationStatusCode => stationStatusCode;

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
            stationStatusCode = 0;
            errorMsg = String.Empty;
            UIUpdate();
        }

        #region workorder
        
        public virtual int WorkorderAmount => throw new NotImplementedException();
        //public virtual bool WorkorderAmountValid => throw new NotImplementedException();
        public virtual bool CanDeployWorkorder => throw new NotImplementedException();
        public virtual bool Canrun => throw new NotImplementedException();
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
        public virtual RequestResult CheckCanAddItem() => throw new NotImplementedException();
        public bool CanStationIn => CheckCanAddItem().IsSuccess;
        public virtual RequestResult AddItemDetail(ItemDetail itemDetail) => throw new NotImplementedException();
        
        

        public virtual RequestResult CheckCanRemoveItem() => throw new NotImplementedException();
        public bool CanStationOut => CheckCanRemoveItem().IsSuccess;
        
        #endregion

        #region task
        protected virtual int WIPTaskAmount => throw new NotImplementedException();
        //public virtual bool TaskAmountValid => throw new NotImplementedException();
        #endregion

        #region task operation

        public virtual RequestResult CheckCanAddTask() => throw new NotImplementedException();
        public virtual RequestResult AddTaskDetail(TaskDetail taskDetail) => throw new NotImplementedException();

        public virtual RequestResult CheckCanRemoveTask() => throw new NotImplementedException();
        //ublic bool CanStationOut => CheckCanRemoveItem().IsSuccess;

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
