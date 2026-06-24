using System;
using System.ComponentModel.DataAnnotations;

namespace MyEccomerce.Models
{
    public class UserToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Isumpay sa user nga nag-log in

        [Required]
        public string Token { get; set; } // Ang taas nga Firebase Device Token

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}