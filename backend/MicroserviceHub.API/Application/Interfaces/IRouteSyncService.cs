using MicroserviceHub.API.Application.DTOs.Response;

namespace MicroserviceHub.API.Application.Interfaces
{
    public interface IRouteSyncService
    {
        Task<RouteSyncResult> SyncAsync();
    }
}