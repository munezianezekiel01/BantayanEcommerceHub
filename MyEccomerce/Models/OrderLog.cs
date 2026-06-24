using System.ComponentModel.DataAnnotations.Schema;

namespace MyEccomerce.Models
{
    public class OrderLog
    {
        public int OrderLogId { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; } // e.g., "Pending", "Confirmed"
        public string Note { get; set; }   // e.g., "Order has been confirmed by seller"
        public DateTime LogDate { get; set; } = DateTime.Now;

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

    }
}
