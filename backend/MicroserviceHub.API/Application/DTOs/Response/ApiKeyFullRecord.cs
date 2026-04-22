namespace MicroserviceHub.API.Application.DTOs.Response
{
    public class ApiKeyFullRecord
    {
        public int    Id                   { get; set; }
        public int    ApplicationId        { get; set; }
        public string Environment          { get; set; } = string.Empty;
        public string AppKey               { get; set; } = string.Empty;
        public string AppSecretHash        { get; set; } = string.Empty;
        public bool   IsActive             { get; set; }
        public bool   IsEnvironmentEnabled { get; set; }
    }
}