using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.ShopfloorPKG
{
    public class ItemRecord
    {
        public Guid Id { get; set; }
        public Guid ItemId { get; set; }
        public string RecordName { get; set; } = null!;
        public string RecordValue { get; set; } = null!;
        public virtual ItemDetail? ItemDetail { get; set; }
    }
}
