namespace MicroserviceHub.API.Application.DTOs.Response
{
    public class GetApplicationDetailsResponse
    {
        public int ApplicationId { get; set; }
        public string Title { get; set; } = "";

        public List<EnvironmentDto> Environments { get; set; } = new();
        public List<MicroserviceDto> Microservices { get; set; } = new();
    }

    public class EnvironmentDto
    {
        public int Id { get; set; }           // ApiKeys.Id — used for regenerate/revoke keyId
        public string Environment { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string ApiSecret { get; set; } = "";
        public bool IsEnabled { get; set; }
    }

    public class MicroserviceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsEnabled { get; set; }
    }
}
