

using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Domain.Entities;
using MicroserviceHub.API.Infrastructure.ExternalServices;


namespace MicroserviceHub.API.Application.Interfaces
{
    public interface IApplicationRepository
    {
        Task<int> CreateApplication(Domain.Entities.Application app);
        Task CreateApiKey(int appId, string environment, string apiKey, string apiSecretHash);

        Task<List<GetApplicationResponse>> GetApplicationsByUser(int userId);
        Task<List<GetApplicationResponse>> GetAllApplications();

        Task DeleteApplication(int appId);
        Task<Domain.Entities.Application> GetApplicationById(int appId);

        Task<GetApplicationDetailsResponse> GetApplicationDetails(int appId);

        Task UpdateEnvironment(int appId, string environment, bool isEnabled);
        Task UpsertMicroservice(int appId, int microserviceId, bool isEnabled);

        Task UpdateApiSecret(int keyId, string newSecret);
        Task RevokeApiKey(int keyId);
        Task<IEnumerable<Microservice>> GetMicroservicesAsync();
        Task<ApiKeyInfoResponse> GetApiKeyById(int keyId);
        Task UpdateApiKeyAndSecret(int keyId, string newAppKey, string newAppSecret);
        Task BeginTransaction();
        Task CommitTransaction();
        Task RollbackTransaction();
    }
} 
