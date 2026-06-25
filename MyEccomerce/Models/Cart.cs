namespace MyEccomerce.Models
{
    public class Cart
    {
        public int CartId { get; set; }

        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime DateAdded { get; set; }

        // Navigation Properties (Para dali ra i-Join)
        public virtual User User { get; set; }
        public virtual Product Product { get; set; }
        public int? VariantId { get; set; }

        public virtual ProductVariant Variant { get; set; }
    }
}
