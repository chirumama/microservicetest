namespace MicroserviceHub.API.Application.DTOs.Request
{
    /// <summary>
    /// Sent by admin to update which individual routes are enabled
    /// for a given microservice in a given application.
    /// PUT /Application/{appId}/microservices/{msId}/routes
    /// </summary>
    public class UpdateRouteAccessRequest
    {
        public List<RouteToggle> Routes { get; set; } = new();
    }

    public class RouteToggle
    {
        public string RouteId   { get; set; } = string.Empty;
        public bool   IsEnabled { get; set; }
    }
}