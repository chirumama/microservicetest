namespace MicroserviceHub.API.Application.DTOs.Response
{
    public class GetApplicationResponse
    {
        public int Id { get; set; }
        public required string  Title { get; set; }
        public required string Description { get; set; }
        public string OwnerEmail { get; set; }
    }
} 
 