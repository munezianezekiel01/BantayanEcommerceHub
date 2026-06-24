namespace MyEccomerce.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        public int UserId { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } // "Pending", "Confirmed", "Out for Delivery", "Completed", "Cancelled"

        public string DeliveryAddress { get; set; }

        // --- MAO NI ANG DUGANG PARA SA ARCHIVE/SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;

        // --- MGA GIDUGANG PARA SA TRACKING ---

        // Kinsa nga Rider ang gi-assign (I-match ni sa UserId sa rider)



        public int? RiderId { get; set; }

        // Current Location sa Rider (I-update ni real-time via SignalR)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // --- RELATIONSHIPS ---

        public virtual ICollection<OrderItem> OrderItems { get; set; }

        public virtual User User { get; set; }

        public virtual ICollection<OrderLog> OrderLogs { get; set; }

        // [ForeignKey("RiderId")]
        // public virtual User Rider { get; set; }
    }
}