using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public abstract partial class Tag
    {
        public Guid Id { get; set; }

        public Guid CategoryId { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        [Range(1, 44)]
        public virtual int DataType { get; set; }

        public bool UpdateByTime { get; set; } = true;

        public virtual TagCategory Category { get; set; } = null!;

        public virtual ICollection<TagWarningCondition> TagWarningConditions { get; set; } = new List<TagWarningCondition>();
    }
}
