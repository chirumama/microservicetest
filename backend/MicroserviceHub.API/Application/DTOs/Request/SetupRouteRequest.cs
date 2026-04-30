namespace MicroserviceHub.API.Application.DTOs.Request
{
    public class SetupRouteRequest
    {
        public string  RouteId      { get; set; } = string.Empty;
        public string  UriPattern   { get; set; } = string.Empty;
        public string  UpstreamId   { get; set; } = string.Empty;
        public string  Host         { get; set; } = string.Empty;
        public int     Port         { get; set; }
        public string? RewriteFrom  { get; set; }  // optional — gateway path pattern
        public string? RewriteTo    { get; set; }  // optional — internal service path
    }
}