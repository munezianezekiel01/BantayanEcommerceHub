using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyEccomerce.Models
{
    [Table("Products", Schema = "dbo")]
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        // Butangi og ? ang string kay sa SQL, ang nvarchar(max) pwede ma-null
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        public int Stock { get; set; }

        public int SoldCount { get; set; }

        public int CategoryId { get; set; }

        public bool IsFlashDeal { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Importante: Ang decimal ug int dapat nullable (?) kung naay existing rows nga null ni sa SQL
        public decimal? BulkPrice { get; set; }
        public int? BulkThreshold { get; set; }

        // Butangi og ? ang Category para dili mag-error kung wala pay Category ang product
        public virtual Category? Category { get; set; }

        // Kani ang UnitName, dapat gyud ni nullable string
        public string? UnitName { get; set; }

        public int ViewCount { get; set; } = 0;
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    
    
    
    }
}