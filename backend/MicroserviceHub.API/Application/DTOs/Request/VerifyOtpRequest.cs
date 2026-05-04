namespace MicroserviceHub.API.Application.DTOs.Request
{
    public class VerifyOtpRequest
    {
        public int UserId { get; set; }
        public string Otp { get; set; } = string.Empty;
    }
}