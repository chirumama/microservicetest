using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Domain.Entities;
using Serilog;

namespace MicroserviceHub.API.Application.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplicationRepository _repository;

        public ApplicationService(IApplicationRepository repository)
        {
            _repository = repository;
        }

        public async Task<CreateApplicationResponse> CreateApplicationAsync(CreateApplicationRequest request)
        {
            Log.Information("Creating application for UserId: {UserId}", request.UserId);

            var app = new Domain.Entities.Application
            {
                UserId      = request.UserId,
                Title       = request.Title,
                Description = request.Description,
                Status      = 1,
                CreatedAt   = DateTime.UtcNow
            };

            var appId = await _repository.CreateApplication(app);

            Log.Information("Application created with Id: {AppId}", appId);

            // Create API keys for all three environments
            // Match the exact environment names the frontend uses
            var environments = new[] { "Development", "Pre-Production", "Production" };

            string devKey = "", devSecret = "";

            foreach (var env in environments)
            {
                // Use N-format GUIDs (no hyphens) for cleaner keys
                var appKey    = "ak_" + Guid.NewGuid().ToString("N");
                var appSecret = "sk_" + Guid.NewGuid().ToString("N");

                if (env == "Development")
                {
                    devKey    = appKey;
                    devSecret = appSecret;
                }

                await _repository.CreateApiKey(appId, env, appKey, appSecret);

                Log.Information("API Key generated for AppId: {AppId}, Environment: {Env}", appId, env);
            }

            // Return the real Development credentials (not hardcoded placeholders)
            return new CreateApplicationResponse
            {
                ApplicationId = appId,
                AppKey        = devKey,
                AppSecret     = devSecret
            };
        }

        public async Task<List<GetApplicationResponse>> GetApplicationsAsync(int userId, int roleId)
        {
            // DB roles: 1=User, 2=Admin, 3=SuperAdmin
            // Admin (2) and SuperAdmin (3) see all apps; User (1) sees only their own
            if (roleId == 2 || roleId == 3)
            {
                return await _repository.GetAllApplications();
            }

            return await _repository.GetApplicationsByUser(userId);
        }

        public async Task<GetApplicationDetailsResponse> GetApplicationDetailsAsync(int appId, int userId, int roleId)
{
    // Users can only view their own apps
    if (roleId == 1)
    {
        var app = await _repository.GetApplicationById(appId);
        if (app == null || app.UserId != userId)
            throw new UnauthorizedAccessException("Access denied");
    }

    return await _repository.GetApplicationDetails(appId);
}

        public async Task UpdateApplicationSettingsAsync(int appId, UpdateApplicationSettingsRequest request, int userId)
        {
            Log.Information("Updating settings for AppId: {AppId} by UserId: {UserId}", appId, userId);

            await _repository.BeginTransaction();

            try
            {
                foreach (var micro in request.Microservices)
                {
                    Log.Information("Updating Microservice: {MicroId} -> Enabled: {IsEnabled}", micro.Id, micro.IsEnabled);
                    await _repository.UpsertMicroservice(appId, micro.Id, micro.IsEnabled);
                }

                foreach (var env in request.Environments)
                {
                    Log.Information("Updating Environment: {Env} -> Enabled: {IsEnabled}", env.Name, env.IsEnabled);
                    await _repository.UpdateEnvironment(appId, env.Name, env.IsEnabled);
                }

                await _repository.CommitTransaction();

                Log.Information("Settings updated successfully for AppId: {AppId}", appId);
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransaction();
                Log.Error(ex, "Failed to update settings for AppId: {AppId}", appId);
                throw;
            }
        }

        public async Task RegenerateSecretAsync(int keyId)
        {
            Log.Information("Regenerating API Secret for KeyId: {KeyId}", keyId);
            var newSecret = "sk_" + Guid.NewGuid().ToString("N");
            await _repository.UpdateApiSecret(keyId, newSecret);
        }

        public async Task RevokeKeyAsync(int keyId)
        {
            Log.Warning("Revoking API Key: {KeyId}", keyId);
            await _repository.RevokeApiKey(keyId);
        }

        public async Task<IEnumerable<Microservice>> GetMicroservicesAsync()
        {
            return await _repository.GetMicroservicesAsync();
        }
    }
}
