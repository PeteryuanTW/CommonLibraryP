using CommonLibraryP.API;
using CommonLibraryP.Data;
using CommonLibraryP.MachinePKG;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.ShopfloorPKG
{
    public class StationSingleWorkorderSingleSerial : StationSingleWorkorder
    {


        public StationSingleWorkorderSingleSerial(Station station) : base(station)
        {

        }

        protected ItemDetail? wipItemDetail;
        [NotMapped]
        public ItemDetail? WIPItemDetail => wipItemDetail;
        public override int WIPItemAmount => wipItemDetail is null ? 0 : 1;


        protected TaskDetail? wipTaskDetail;
        [NotMapped]
        public TaskDetail? WIPTaskDetail => wipTaskDetail;
        protected override int WIPTaskAmount => WIPTaskDetail is null ? 0 : 1;

        public override bool CanClear => IsRunning && WorkorderAmount is 1 && WIPItemAmount is 0 && WIPTaskAmount is 0;

        public override RequestResult CheckCanAddItem()
        {
            var res = base.CheckCanAddItem();
            if(!res.IsSuccess)
            {
                return res;
            }

            if (WorkorderAmount is not 1)
            {
                return new RequestResult(4, $"Station {Name} workorder amount error ({WorkorderAmount})");
            }
            if (WIPItemAmount is not 0)
            {
                return new RequestResult(4, $"Station {Name} item amount error ({WIPItemAmount})");
            }
            if (WIPTaskAmount is not 0)
            {
                return new RequestResult(4, $"Station {Name} task amount error ({WIPTaskAmount})");
            }
            return new RequestResult(2, $"Station {Name} can add item");
        }
        public override RequestResult AddItemDetail(ItemDetail itemDetail)
        {
            var check = CheckCanAddItem();
            if (!check.IsSuccess)
            {
                return check;
            }
            if (WIPItemAmount is not 0)
            {
                return new RequestResult(4, $"Item {itemDetail.SerialNo} task amount {itemDetail.TaskDetails.Count} error");
            }
            wipItemDetail = itemDetail;
            UIUpdate();
            return new RequestResult(2, $"Station {Name} add item {itemDetail?.SerialNo} success");
        }
        public override RequestResult CheckCanRemoveItem()
        {
            var res = base.CheckCanRemoveItem();
            if (!res.IsSuccess)
            {
                return res;
            }
            if (WorkorderAmount is not 1)
            {
                return new RequestResult(4, $"Station {Name} has no workorder yet");
            }
            if (WIPItemAmount is not 1)
            {
                return new RequestResult(4, $"Station {Name} has no item yet");
            }
            //if (WIPTaskAmount is not 1)
            //{
            //    return new RequestResult(4, $"Station {Name} task amount error {WIPTaskAmount}");
            //}
            return new RequestResult(2, $"Station {Name} can remove item");
        }
        public RequestResult RemoveItemDetail()
        {
            var check = CheckCanRemoveItem();
            if (!check.IsSuccess)
            {
                return check;
            }
            if (WIPItemAmount is not 1)
            {
                return new RequestResult(4, $"Item amount {WIPItemAmount} error"); ;
            }
            wipItemDetail = null;
            UIUpdate();
            return new RequestResult(2, $"Station {Name} remove item success");
        }
        public override RequestResult CheckCanAddTask()
        {
            var res = base.CheckCanAddTask();
            if (!res.IsSuccess)
            {
                return res;
            }
            if (WorkorderAmount is not 1)
            {
                return new RequestResult(4, $"Station {Name} workorder amount error ({WorkorderAmount})");
            }
            if (WIPItemAmount is not 1)
            {
                return new RequestResult(4, $"Station {Name} item amount error ({WIPItemAmount})");
            }
            if (WIPTaskAmount is not 0)
            {
                return new RequestResult(4, $"Station {Name} task amount error ({WIPTaskAmount})");
            }
            return new RequestResult(2, $"Station {Name} can add task");
        }
        
        public override RequestResult AddTaskDetail(TaskDetail taskDetail)
        {
            var check = CheckCanAddTask();
            if (!check.IsSuccess)
            {
                return check;
            }
            //if (WIPItemAmount is not 1)
            //{
            //    return new RequestResult(4, $"Item amount {WIPItemAmount} error");
            //}
            //if (WIPTaskAmount is not 0)
            //{
            //    return new RequestResult(4, $"Task amount {WIPTaskAmount} error");
            //}
            wipTaskDetail = taskDetail;
            UIUpdate();
            return new RequestResult(2, $"Station add task success");
        }
        public override RequestResult CheckCanRemoveTask()
        {
            var check = base.CheckCanRemoveTask();
            if (!check.IsSuccess)
            {
                return check;
            }
            if (WorkorderAmount is not 1)
            {
                return new RequestResult(4, $"Station {Name} has no workorder yet");
            }
            if (WIPItemAmount is not 0)
            {
                return new RequestResult(4, $"Station {Name} has item now");
            }
            if (WIPTaskAmount is not 1)
            {
                return new RequestResult(4, $"Station {Name} task amount error {WIPTaskAmount}");
            }
            return new RequestResult(2, $"Station {Name} can remove task");
        }
        public RequestResult RemoveTaskDetail()
        {
            var check = CheckCanRemoveTask();
            if (!check.IsSuccess)
            {
                return check;
            }
            //if (WIPItemAmount is not 1)
            //{
            //    return new RequestResult(4, $"Item amount {WIPItemAmount} error"); ;
            //}
            //if (WIPTaskAmount is not 1)
            //{
            //    return new RequestResult(4, $"Item task amount {WIPTaskAmount} error"); ;
            //}
            wipTaskDetail = null;
            UIUpdate();
            return new RequestResult(2, $"Station {Name} remove item success");
        }



        public override bool CheckItemIsWIP(string serialNo)
        {
            return wipItemDetail?.SerialNo == serialNo;
        }
        public override RequestResult RefreshItemAndRecord(ItemDetail itemDetail)
        {
            wipItemDetail = itemDetail;
            UIUpdate();
            return new RequestResult(2, $"Station {Name} refresh item {itemDetail?.SerialNo} success");
        }
    }
}
