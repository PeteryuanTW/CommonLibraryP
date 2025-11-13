using CommonLibraryP.API;
using DevExpress.Blazor.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.ShopfloorPKG
{
    public class StationSingleWorkorderMultipleSerials : StationSingleWorkorder
    {
        public StationSingleWorkorderMultipleSerials(Station station) : base(station)
        {
        }

        protected List<ItemDetail> wipItemDetails = new();
        [NotMapped]
        public List<ItemDetail> WIPItemDetails => wipItemDetails;
        public override int WIPItemAmount => wipItemDetails.Count;

        protected List<TaskDetail> wipTaskDetails = new();
        [NotMapped]
        public List<TaskDetail> WIPTaskDetails => wipTaskDetails;
        protected override int WIPTaskAmount => wipTaskDetails.Count;

        public override bool CanClear => IsRunning && WorkorderAmount is 1 && WIPItemAmount is 0 && WIPTaskAmount is 0;

        public override RequestResult CheckCanAddItem()
        {
            var res = base.CheckCanAddItem();
            if (!res.IsSuccess)
            {
                return res;
            }

            if (WorkorderAmount is not 1)
            {
                return new RequestResult(4, $"Station {Name} workorder amount error ({WorkorderAmount})");
            }
            if (WIPItemAmount != WIPTaskAmount)
            {
                return new RequestResult(4, $"Station {Name} item amount ({WIPItemAmount}) not equal to task amount ({WIPTaskAmount})");
            }
            //if (WIPItemAmount is not 0)
            //{
            //    return new RequestResult(4, $"Station {Name} item amount error ({WIPItemAmount})");
            //}
            //if (WIPTaskAmount is not 0)
            //{
            //    return new RequestResult(4, $"Station {Name} task amount error ({WIPTaskAmount})");
            //}

            return new RequestResult(2, $"Station {Name} can add item");
        }

        public override RequestResult AddItemDetail(ItemDetail itemDetail)
        {
            var check = CheckCanAddItem();
            if (!check.IsSuccess)
            {
                return check;
            }
            //if (WIPItemAmount is not 0)
            //{
            //    return new RequestResult(4, $"Item {itemDetail.SerialNo} task amount {itemDetail.TaskDetails.Count} error");
            //}
            wipItemDetails.Add(itemDetail);
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
            if (WIPItemAmount is 0)
            {
                return new RequestResult(4, $"Station {Name} has no item yet");
            }
            //if (WIPTaskAmount is not 1)
            //{
            //    return new RequestResult(4, $"Station {Name} task amount error {WIPTaskAmount}");
            //}
            return new RequestResult(2, $"Station {Name} can remove item");
        }

        public RequestResult<ItemDetail> RemoveItemDetail()
        {
            var check = CheckCanRemoveItem();
            if (!check.IsSuccess)
            {
                return new(check.ReturnCode, check.Msg, null);
            }
            var target = wipItemDetails.OrderBy(x => x.StartTime).FirstOrDefault();
            if (target is null)
            {
                return new(4, $"No WIP item", null); ;
            }
            wipItemDetails.Remove(target);
            //wipItemDetail = null;
            UIUpdate();
            return new RequestResult<ItemDetail>(2, $"Station {Name} remove item success", target);
        }

        public RequestResult<ItemDetail> RemoveItemDetail(string serialNo)
        {
            var check = CheckCanRemoveItem();
            if (!check.IsSuccess)
            {
                return new(check.ReturnCode, check.Msg, null);
            }
            if (WIPItemAmount is 0)
            {
                return new(4, $"No WIP item", null); ;
            }
            var target = wipItemDetails.FirstOrDefault(x => x.SerialNo == serialNo);
            if (target is null)
            {
                return new(4, $"No WIP item serial no is {serialNo}", null); ;
            }
            wipItemDetails.Remove(target);
            //wipItemDetail = null;
            UIUpdate();
            return new RequestResult<ItemDetail>(2, $"Station {Name} remove item success", target);
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
            if (WIPItemAmount is 0)
            {
                return new RequestResult(4, $"Station {Name} item amount is 0");
            }
            if (WIPTaskAmount != WIPItemAmount - 1)
            {
                return new RequestResult(4, $"Station {Name} task amount error (item amount:{WIPItemAmount} task amount:{WIPTaskAmount})");
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
            wipTaskDetails.Add(taskDetail);
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
            if (WIPItemAmount != WIPTaskAmount - 1)
            {
                return new RequestResult(4, $"Station {Name} item amount({WIPItemAmount}) not equal to task amount({WIPTaskAmount})");
            }
            if (WIPTaskAmount is 0)
            {
                return new RequestResult(4, $"Station {Name} task amount is 0");
            }
            return new RequestResult(2, $"Station {Name} can remove task");
        }

        public RequestResult<TaskDetail> RemoveTaskDetail()
        {
            var check = CheckCanRemoveTask();
            if (!check.IsSuccess)
            {
                return new(check.ReturnCode, check.Msg, null);
            }
            //if (WIPItemAmount is not 1)
            //{
            //    return new RequestResult(4, $"Item amount {WIPItemAmount} error"); ;
            //}
            //if (WIPTaskAmount is not 1)
            //{
            //    return new RequestResult(4, $"Item task amount {WIPTaskAmount} error"); ;
            //}
            var target = wipTaskDetails.OrderBy(x => x.StartTime).FirstOrDefault();
            if (target is null)
            {
                return new(4, $"No WIP task", null); ;
            }
            wipTaskDetails.Remove(target);
            UIUpdate();
            return new(2, $"Station {Name} remove item success", target);
        }

        public RequestResult<TaskDetail> RemoveTaskDetail(Guid itemId)
        {
            var check = CheckCanRemoveTask();
            if (!check.IsSuccess)
            {
                return new(check.ReturnCode, check.Msg, null);
            }
            var target = wipTaskDetails.FirstOrDefault(x => x.ItemId == itemId);
            if (target is null)
            {
                return new(4, $"No WIP task item is is {itemId}", null); ;
            }
            wipTaskDetails.Remove(target);
            //wipItemDetail = null;
            UIUpdate();
            return new(2, $"Station {Name} remove item success", target);
        }

        public override bool CheckItemIsWIP(string serialNo)
        {
            return wipItemDetails.Any(x => x.SerialNo == serialNo);
        }
        public override RequestResult RefreshItemAndRecord(ItemDetail itemDetail)
        {
            var target = wipItemDetails.FirstOrDefault(x => x.SerialNo == itemDetail.SerialNo);
            if (target is not null)
            {
                UIUpdate();
                return new RequestResult(2, $"Station {Name} refresh item {itemDetail?.SerialNo} success");
            }
            else
            {
                return new RequestResult(4, $"Station {Name} item serial no {itemDetail?.SerialNo} not found");

            }

        }
    }
}
