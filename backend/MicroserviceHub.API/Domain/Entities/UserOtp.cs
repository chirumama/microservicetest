namespace MicroserviceHub.API.Domain.Entities
{
    public class UserOtp
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string OtpCode { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}