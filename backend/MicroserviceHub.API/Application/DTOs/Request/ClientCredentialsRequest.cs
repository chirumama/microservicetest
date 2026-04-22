namespace MicroserviceHub.API.Application.DTOs.Request
{
    public class ClientCredentialsRequest
    {
        public string GrantType    { get; set; } = string.Empty; // "client_credentials"
        public string ClientId     { get; set; } = string.Empty; // AppKey  (ak_...)
        public string ClientSecret { get; set; } = string.Empty; // AppSecret (sk_...)
        public string? Environment { get; set; }                  // "Development" | "Pre-Production" | "Production"
    }
}