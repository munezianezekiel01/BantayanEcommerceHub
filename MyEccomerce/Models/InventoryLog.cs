using System.ComponentModel.DataAnnotations;

namespace MyEccomerce.Models
{
    public class InventoryLog
    {
        [Key]
        public int LogId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } // Positive if Stock In, Negative if Stock Out
        public string Type { get; set; } // "Restock", "Sale", "Adjustment", "Damage"
        public string Remarks { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public virtual Product Product { get; set; }
    }
}
