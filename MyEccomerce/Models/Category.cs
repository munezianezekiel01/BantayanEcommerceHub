using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyEccomerce.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }
        public string IconClass { get; set; }

        // --- KINI ANG NAWALA NGA LINYA ---
        // Kinahanglan naay 'int?' (nullable) para mosugot ang C# nga naay NULL sa SQL
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public virtual Category Parent { get; set; }

        // Listahan sa mga 'Anak' o Sub-categories
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    }
}