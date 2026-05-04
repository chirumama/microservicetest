using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Domain.Entities;
using MicroserviceHub.API.Infrastructure.ExternalServices;

namespace MicroserviceHub.API.Application.Interfaces
{
    public interface IApplicationRepository
    {
        Task<int>    CreateApplication(Domain.Entities.Application app);
        Task         DeleteApplication(int appId);
        Task         UpdateApiKeyAndSecret(int keyId, string newAppKey, string newAppSecret);
        Task         CreateApiKey(int appId, string environment, string apiKey, string apiSecret);
        Task<List<GetApplicationResponse>>   GetApplicationsByUser(int userId);
        Task<List<GetApplicationResponse>>   GetAllApplications();
        Task<GetApplicationDetailsResponse>  GetApplicationDetails(int appId);
        Task         UpdateEnvironment(int appId, string environment, bool isEnabled);
        Task<ApiKeyFullRecord?>  GetApiKeyByAppKey(string appKey);
        Task         UpsertMicroservice(int appId, int microserviceId, bool isEnabled);
        Task         UpdateApiSecret(int keyId, string newSecret);
        Task         RevokeApiKey(int keyId);
        Task<IEnumerable<Microservice>> GetMicroservicesAsync();
        Task<Domain.Entities.Application?> GetApplicationById(int appId);
        Task         BeginTransaction();
        Task         CommitTransaction();
        Task         RollbackTransaction();
        Task<ApiKeyInfoResponse> GetApiKeyById(int keyId);
        Task         SaveAccessToken(int keyId, string accessToken);
        Task<string?> GetAccessToken(int keyId);
        Task         UpdateConsumerKey(int keyId, string newConsumerKey);
        Task<string> GetConsumerKeyByAppKey(string appKey);

        // Route-level access control
        Task<List<MicroserviceRouteDto>>   GetRoutesForAppAsync(int appId, int microserviceId);
        Task<List<MicroserviceRoute>>      GetMicroserviceRoutesAsync(int microserviceId);
        Task         UpsertApplicationRouteAsync(int appId, int microserviceId, string routeId, bool isEnabled);
        Task<List<string>> GetEnabledRouteIdsAsync(int appId, int microserviceId);
    }
}