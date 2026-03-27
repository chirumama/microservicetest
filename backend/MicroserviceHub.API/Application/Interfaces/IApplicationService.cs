using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Domain.Entities;

namespace MicroserviceHub.API.Application.Interfaces
{
    public interface IApplicationService
    {

        Task<CreateApplicationResponse> CreateApplicationAsync(CreateApplicationRequest request);

        Task<List<GetApplicationResponse>> GetApplicationsAsync(int userId, int roleId);

      Task<GetApplicationDetailsResponse> GetApplicationDetailsAsync(int appId, int userId, int roleId);

        Task UpdateApplicationSettingsAsync(int appId, UpdateApplicationSettingsRequest request, int userId);

        Task RegenerateSecretAsync(int keyId);
        Task RevokeKeyAsync(int keyId);
        Task<IEnumerable<Microservice>> GetMicroservicesAsync();

    }  
}
