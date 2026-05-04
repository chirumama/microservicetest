namespace MicroserviceHub.API.Domain.Entities
{
    public class MicroserviceRoute
    {
        public int     Id             { get; set; }
        public int     MicroserviceId { get; set; }
        public string  RouteId        { get; set; } = string.Empty;
        public string  Method         { get; set; } = string.Empty;
        public string  Path           { get; set; } = string.Empty;
        public string? Description    { get; set; }
        public bool    IsActive       { get; set; } = true;
        public DateTime CreatedAt     { get; set; }
    }
}