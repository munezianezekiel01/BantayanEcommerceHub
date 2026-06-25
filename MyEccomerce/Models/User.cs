namespace MyEccomerce.Models
{
    public class User
    {
        public int UserId { get; set; }

        public string? GoogleId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // Gihimo natong Nullable (?) kay dili ni ihatag sa Google sa sugod
        public string? Phone { get; set; }

        // Nullable gihapon kay kung Google Login, wala tay password makuha
        public string? Password { get; set; }

        public DateTime DateCreated { get; set; }

        public string? ImageUrl { get; set; }

        public string? Address { get; set; }

        // --- MUNI ANG MGA SECURITY FIELDS NGA ATONG I-ADD ---

        // Para sa 6-digit code
        public string? CurrentOTP { get; set; }

        // Kanus-a ma-expire ang code (pananglitan 10 mins)
        public DateTime? OtpExpiry { get; set; }

        // Dire i-save ang "Fingerprint" sa browser o phone (User-Agent)
        public string? LastDeviceFingerprint { get; set; }

        public string? FacebookId { get; set; }


        public string? UserType { get; set; }// I-allow as nullable
        public string? Gender { get; set; }
    }
}