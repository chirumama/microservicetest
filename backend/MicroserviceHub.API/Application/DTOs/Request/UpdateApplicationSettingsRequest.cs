namespace MicroserviceHub.API.Application.DTOs.Request
{
    public class UpdateApplicationSettingsRequest
    {
        public List<EnvironmentUpdateDto> Environments { get; set; }
        public List<MicroserviceUpdateDto> Microservices { get; set; }
    }

    public class EnvironmentUpdateDto
    {
        public string Name { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class MicroserviceUpdateDto
    {
        public int Id { get; set; }
        public bool IsEnabled { get; set; }
    }
}
