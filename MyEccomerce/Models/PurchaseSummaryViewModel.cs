namespace MyEccomerce.Models
{
    public class PurchaseSummaryViewModel
    {
        public int? OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // List sa mga napalit nga items
        public List<PurchaseSummaryItemViewModel> Items { get; set; } = new();

        // Financial Breakdown
        public decimal SubTotal => Items.Sum(i => i.TotalPrice);
        public decimal ShippingFee { get; set; } = 50.00m; // Default or calculated shipping
        public decimal TotalAmount => SubTotal + ShippingFee;
    }

    public class PurchaseSummaryItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
