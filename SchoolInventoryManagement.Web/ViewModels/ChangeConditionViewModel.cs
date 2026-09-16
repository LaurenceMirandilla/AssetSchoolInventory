using System.ComponentModel.DataAnnotations;
using SchoolInventoryManagement.DAL.Entities.Enums;

namespace SchoolInventoryManagement.Web.ViewModels
{
    // Records a condition an officer observed, outside any assignment,
    // movement or disposal. Every other way the condition changes is a side
    // effect of one of those -- this is the plain "I looked at it and it is
    // worse than we thought" case, which had no screen at all.
    public class ChangeConditionViewModel
    {
        public int AssetID { get; set; }

        [Required]
        [Display(Name = "Condition")]
        public ConditionStatus Condition { get; set; }

        [Required]
        public string RowVersionBase64 { get; set; } = null!;
    }
}
