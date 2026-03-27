namespace MicroserviceHub.API.Application.DTOs.Response
{
    public class LoginResponse
    {
        public required string Token { get; set; }
        public required string Role { get; set; }
    }
}
