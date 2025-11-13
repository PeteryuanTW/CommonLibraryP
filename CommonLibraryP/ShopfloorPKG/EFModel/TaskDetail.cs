using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.ShopfloorPKG
{
    public partial class TaskDetail
    {
        public Guid Id { get; set; }

        public Guid? ItemId { get; set; }

        public Guid? StationId { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? FinishedTime { get; set; }

        public virtual ItemDetail? Item { get; set; }

        public virtual Station? Station { get; set; }

        //public virtual ICollection<TaskRecordDetail> TaskRecordDetails { get; set; } = new List<TaskRecordDetail>();
    }
}
