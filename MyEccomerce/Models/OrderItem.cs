namespace MyEccomerce.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }
        public virtual Order Order { get; set; } // Maayo ni nga naay reference sa parent Order

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        // --- KINI NGA BAG-O NGA LINE ---
        public int? VariantId { get; set; } // Nullable kay dili tanan product naay variation
        public virtual ProductVariant Variant { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; } // Price at the time of purchase (Important!)
    }
}