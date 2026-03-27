namespace MicroserviceHub.API.Application.DTOs.Request
{
    public class CreateUserRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public int RoleId { get; set; } 
    } 
}
