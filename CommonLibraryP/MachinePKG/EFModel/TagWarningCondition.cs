using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public abstract partial class TagWarningCondition
    {
        public Guid Id { get; set; }
        public Guid TagId { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        public int ComparisonCode { get; set; }
        public string WarningMessage { get; set; } = string.Empty;

        public abstract string TargetValueString { get;}

        public virtual Tag Tag { get; set; } = null!;
    }
}
