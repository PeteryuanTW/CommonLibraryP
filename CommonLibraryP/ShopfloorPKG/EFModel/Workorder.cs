using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CommonLibraryP.ShopfloorPKG
{
    public partial class Workorder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        [StringLength(50)]
        public string WorkorderNo { get; set; } = null!;
        [Required]
        [StringLength(50)]
        public string Lot { get; set; } = null!;
        [Required]
        public Guid ProcessId { get; set; }
        public int Status { get; set; }
        public Guid? RecipeCategoryId { get; set; }
        public Guid? WorkorderRecordCategoryId { get; set; }
        public Guid? ItemRecordsCategoryId { get; set; }
        public Guid? TaskRecordCategoryId { get; set; }
        [Required]
        [MaxLength(50)]
        public string? PartNo { get; set; }
        public int TargetAmount { get; set; }
        
        public int OkAmount { get; set; }
        public int NgAmount { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? FinishedTime { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public DateTime CreateTime { get; set; } = DateTime.Now;



        public virtual ICollection<ItemDetail> ItemDetails { get; set; } = new List<ItemDetail>();

        //public virtual ItemRecordConfig? ItemRecordsCategory { get; set; }

        public virtual Process Process { get; set; } = null!;

        //public virtual WorkorderRecipeConfig? RecipeCategory { get; set; }

        //public virtual TaskRecordConfig? TaskRecordCategory { get; set; }

        public virtual WorkorderRecordConfig? WorkorderRecordCategory { get; set; }

        public virtual ICollection<WorkorderRecordDetail> WorkorderRecordDetails { get; set; } = new List<WorkorderRecordDetail>();
    }
}
