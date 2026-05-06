namespace MicroserviceHub.API.Application.DTOs.Response
{
    public class RouteSyncResult
    {
        public bool          Success      { get; set; }
        public int           TotalRoutes  { get; set; }  // routes found in APISix
        public int           Synced       { get; set; }  // upserted into DB
        public int           Skipped      { get; set; }  // missing required labels
        public List<string>  Added        { get; set; } = new();   // newly inserted
        public List<string>  Updated      { get; set; } = new();   // updated existing
        public List<string>  SkipReasons  { get; set; } = new();   // why skipped
        public DateTime      SyncedAt     { get; set; } = DateTime.UtcNow;
        public string        Error        { get; set; } = string.Empty;
    }
}