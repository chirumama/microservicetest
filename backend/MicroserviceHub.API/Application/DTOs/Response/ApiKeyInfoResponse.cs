namespace MicroserviceHub.API.Application.DTOs.Response
{
    public class ApiKeyInfoResponse
    {
        public int Id { get; set; }
        public int ApplicationId { get; set; }
        public string Environment { get; set; } = "";
        public string AppKey { get; set; } = "";
    }
}