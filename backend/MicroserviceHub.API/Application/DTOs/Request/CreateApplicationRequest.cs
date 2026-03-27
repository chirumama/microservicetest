namespace MicroserviceHub.API.Application.DTOs.Request
{
    public class CreateApplicationRequest
    {
         
        public int UserId { get; set; } 
        public required string Title { get; set; }
        public required string Description { get; set; }

    }
}
