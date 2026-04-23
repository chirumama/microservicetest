using System.Text.Json.Serialization;

namespace MicroserviceHub.API.Application.DTOs.Request
{
    public class ClientCredentialsRequest
    {
        [JsonPropertyName("grant_type")]
        public string GrantType { get; set; } = string.Empty;

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; } = string.Empty;

        [JsonPropertyName("environment")]
        public string? Environment { get; set; }
    }
}