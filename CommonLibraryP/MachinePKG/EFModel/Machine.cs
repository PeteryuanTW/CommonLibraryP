using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibraryP.MachinePKG
{
    public partial class Machine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();

        //public Guid? ProcessId { get; set; }
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        [RegularExpression(@"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)")]
        public string Ip { get; set; } = null!;

        [Required]
        [Range(0, 65535)]
        public int Port { get; set; }

        [Required]
        public int ConnectionType { get; set; }

        [Required]
        [Range(-1, 10)]
        public int MaxRetryCount { get; set; }

        public Guid? TagCategoryId { get; set; }

        //public Guid? LogicStatusCategoryId { get; set; }

        //public Guid? ErrorCodeCategoryId { get; set; }

        public bool Enabled { get; set; } = true;

        [Required]
        [Range(100, 65535)]
        public int UpdateDelay { get; set; }

        [Required]
        public bool RecordStatusChanged { get; set; }

        public virtual TagCategory? TagCategory { get; set; }
    }
}
