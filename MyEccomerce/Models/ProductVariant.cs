using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyEccomerce.Models
{
    public class ProductVariant
    {
        [Key]
        public int VariantId { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public string SKU { get; set; } // e.g., "NIKE-RED-42"

        public string VariationName { get; set; } // e.g., "Red, Size 42"

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Stock { get; set; }

        public string? ImageUrl { get; set; }
    }
}
