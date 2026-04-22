namespace MicroserviceHub.API.Application.DTOs.Response
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType   { get; set; } = "Bearer";
        public int    ExpiresIn   { get; set; }
        public string Role        { get; set; } = string.Empty;
        public string Email       { get; set; } = string.Empty;
    }
}