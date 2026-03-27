namespace MicroserviceHub.API.Domain.Entities
{
    public class Application
    {

        public int Id { get; set; }
        public int UserId { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }


    }
}
