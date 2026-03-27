namespace MicroserviceHub.API.Application.DTOs.Response
{
    public class UserSummaryResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}