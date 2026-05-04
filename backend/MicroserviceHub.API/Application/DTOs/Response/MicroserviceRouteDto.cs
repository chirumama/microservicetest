namespace MicroserviceHub.API.Application.DTOs.Response
{
    /// <summary>
    /// A single route within a microservice, with its current enabled state
    /// for a specific application.
    /// </summary>
    public class MicroserviceRouteDto
    {
        public string RouteId     { get; set; } = string.Empty;
        public string Method      { get; set; } = string.Empty;
        public string Path        { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool   IsEnabled   { get; set; } = true;
    }

    /// <summary>
    /// Microservice with its routes — returned by GET /Application/{appId}/microservices/{msId}/routes
    /// </summary>
    public class MicroserviceWithRoutesDto
    {
        public int    MicroserviceId   { get; set; }
        public string MicroserviceName { get; set; } = string.Empty;
        public bool   IsEnabled        { get; set; }
        public List<MicroserviceRouteDto> Routes { get; set; } = new();
    }
}