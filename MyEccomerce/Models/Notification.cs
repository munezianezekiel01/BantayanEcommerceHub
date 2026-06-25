using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyEccomerce.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; } // String ni (ClaimTypes.NameIdentifier)

        public string? Title { get; set; }
        public string? Message { get; set; }

        // Idugang ni para dali makuha ang Product Image sa Join
        public int? OrderId { get; set; }

        public string? TargetUrl { get; set; } // Pananglitan: /Orders/OrderDetails/41

        public string? UserProfilePicture { get; set; } // Avatar sa Admin o Customer

        public string? Status { get; set; } // "Confirmed", "Delivered", etc.

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        // Navigation property (Optional pero nindot para sa LINQ Join)
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }
}