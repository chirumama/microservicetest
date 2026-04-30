namespace MicroserviceHub.API.Application.DTOs.Response
{
    public class ApiKeyInfoResponse
    {
        public int    Id            { get; set; }
        public int    ApplicationId { get; set; }
        public string Environment   { get; set; } = string.Empty;
        public string AppKey        { get; set; } = string.Empty;
        public string ConsumerKey   { get; set; } = string.Empty;
    }
}