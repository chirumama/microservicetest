namespace MicroserviceHub.API.Application.DTOs.Response
{
    /// <summary>
    /// Returned by POST /v1.0.1/auth/login
    /// No JWT token. Client stores UserId and RoleId and sends them
    /// as X-User-Id and X-User-Role headers on every subsequent request.
    /// </summary>
    public class LoginResponse
    {
        public int    UserId { get; set; }
        public int    RoleId { get; set; }
        public string Role   { get; set; } = "";
        public string Email  { get; set; } = "";
    }
}
