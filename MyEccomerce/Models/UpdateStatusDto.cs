namespace MyEccomerce.Models
{
    public class UpdateStatusDto
    {
        private bool success;
        private String message;
        private String currentStatus;

        // Getters ug Setters
        public bool isSuccess() { return success; }
        public String getMessage() { return message; }
        public String getCurrentStatus() { return currentStatus; }
    }
}
