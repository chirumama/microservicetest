using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Domain.Entities;
using MicroserviceHub.API.Infrastructure.ExternalServices;
using Serilog;
using MicroserviceHub.API.Utilities;

namespace MicroserviceHub.API.Application.Services
{
    public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _repository;
    private readonly ApisixService          _apisix;
    private readonly IConfiguration         _config;
    private readonly OAuthTokenService      _tokenService;

    public ApplicationService(
        IApplicationRepository repository,
        ApisixService          apisix,
        IConfiguration         config,
        OAuthTokenService      tokenService)
    {
        _repository   = repository;
        _apisix       = apisix;
        _config       = config;
        _tokenService = tokenService;
    }

        public async Task<CreateApplicationResponse> CreateApplicationAsync(
            CreateApplicationRequest request)
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

            var environments = new[] { "Development", "Pre-Production", "Production" };
            string devKey = "", devSecret = "";

            try
            {
                foreach (var env in environments)
{
    var appKey    = "ak_" + Guid.NewGuid().ToString("N");
    var appSecret = "sk_" + Guid.NewGuid().ToString("N");

    if (env == "Development") { devKey = appKey; devSecret = appSecret; }

    await _repository.CreateApiKey(appId, env, appKey, appSecret);

    var consumerUsername = $"{appId}_{env.Replace("-", "_").Replace(" ", "_")}";
    await _apisix.RegisterConsumerAsync(consumerUsername, appKey, appSecret);

    // Generate permanent token — no expiry, stored in DB
    // Services list is empty at creation — admin hasn't granted anything yet
    var permanentToken = _tokenService.GeneratePermanentApiToken(
        appId:           appId,
        environment:     env,
        consumerUsername: consumerUsername,
        enabledServices: new List<string>());

    // Get the keyId just inserted to save the token
    var keyRecord = await _repository.GetApiKeyByAppKey(appKey);
    if (keyRecord != null)
        await _repository.SaveAccessToken(keyRecord.Id, permanentToken);

    Log.Information("Permanent token generated for AppId: {AppId}, Env: {Env}", appId, env);
}
            }
            catch (Exception ex)
            {
                Log.Error(ex, "APISix registration failed for AppId: {AppId}, rolling back", appId);
                await _repository.DeleteApplication(appId);
                throw;
            }

            return new CreateApplicationResponse
            {
                ApplicationId = appId,
                AppKey        = devKey,
                AppSecret     = devSecret
            };
        }

        public async Task<List<GetApplicationResponse>> GetApplicationsAsync(int userId, int roleId)
        {
            if (roleId == 2 || roleId == 3)
                return await _repository.GetAllApplications();
            return await _repository.GetApplicationsByUser(userId);
        }

        public async Task<GetApplicationDetailsResponse> GetApplicationDetailsAsync(
            int appId, int userId, int roleId)
        {
            if (roleId == 1)
            {
                var app = await _repository.GetApplicationById(appId);
                if (app == null || app.UserId != userId)
                    throw new UnauthorizedAccessException("Access denied");
            }
            return await _repository.GetApplicationDetails(appId);
        }

        public async Task UpdateApplicationSettingsAsync(
            int appId, UpdateApplicationSettingsRequest request, int userId)
        {
            Log.Information("Updating settings for AppId: {AppId} by UserId: {UserId}", appId, userId);

            // Step 1 — update SQL inside a transaction
             var allMicroservices = (await _repository.GetMicroservicesAsync()).ToList();
                var enabled = request.Microservices
    .Where(m => m.IsEnabled)
    .Select(m => allMicroservices.FirstOrDefault(x => x.Id == m.Id)?.Name)
    .Where(n => n != null)
    .Select(n => n!)
    .ToList();

var disabled = request.Microservices
    .Where(m => !m.IsEnabled)
    .Select(m => allMicroservices.FirstOrDefault(x => x.Id == m.Id)?.Name)
    .Where(n => n != null)
    .Select(n => n!)
    .ToList();
            await _repository.BeginTransaction();
            try
            {
               

                foreach (var micro in request.Microservices)
                    await _repository.UpsertMicroservice(appId, micro.Id, micro.IsEnabled);

                foreach (var env in request.Environments)
                    await _repository.UpdateEnvironment(appId, env.Name, env.IsEnabled);

                await _repository.CommitTransaction();
                Log.Information("Settings updated in SQL for AppId: {AppId}", appId);
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransaction();
                Log.Error(ex, "Failed to update settings for AppId: {AppId}", appId);
                throw;
            }

            // Step 2 — sync to APISix route allowlists
            // Load the name->routeId map from config
            var routeMap = _config.GetSection("ApisixRoutes")
                .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();

            // Get all microservice names so we can map Id -> Name
            

            var environments = new[] { "Development", "Pre-Production", "Production" };

            foreach (var micro in request.Microservices)
            {
                // Find the microservice name by Id
                var microName = allMicroservices.FirstOrDefault(m => m.Id == micro.Id)?.Name;
                if (microName == null)
                {
                    Log.Warning("Microservice Id {Id} not found, skipping APISix sync", micro.Id);
                    continue;
                }

                // Find the APISix route ID for this microservice
                if (!routeMap.TryGetValue(microName, out var routeId))
                {
                    Log.Warning("No APISix route configured for microservice '{Name}', skipping", microName);
                    continue;
                }

                // Update all three environment consumers on this route
                // Step 4 — regenerate permanent tokens for each environment
                var details = await _repository.GetApplicationDetails(appId);
foreach (var env in environments)
{
    var envRequest   = request.Environments.FirstOrDefault(e => e.Name == env);
    var isEnvEnabled = envRequest?.IsEnabled ?? false;

    var consumerUsername = $"{appId}_{env.Replace("-", "_").Replace(" ", "_")}";

    var servicesForToken = isEnvEnabled ? enabled : new List<string>();

    var newToken = _tokenService.GeneratePermanentApiToken(
        appId:            appId,
        environment:      env,
        consumerUsername: consumerUsername,
        enabledServices:  servicesForToken);

    // Find the keyId for this app+env to save the token
    
    var envRecord = details.Environments.FirstOrDefault(e => e.Environment == env);
    if (envRecord != null)
        await _repository.SaveAccessToken(envRecord.Id, newToken);

    Log.Information("Permanent token regenerated for AppId: {AppId}, Env: {Env}", appId, env);
}
            }
            // Step 3 — update consumer labels to reflect current service state
// Get the final state of all microservices for this app

// Step 3 — update consumer labels per environment
foreach (var env in environments)
{
    var consumerUsername = $"{appId}_{env.Replace("-", "_").Replace(" ", "_")}";

    // Check if this specific environment is enabled in the request
    var envRequest = request.Environments.FirstOrDefault(e => e.Name == env);
    var isEnvEnabled = envRequest?.IsEnabled ?? false;

    if (isEnvEnabled)
    {
        // Environment is ON — show actual enabled/disabled services
        await _apisix.UpdateConsumerLabelsAsync(consumerUsername, enabled, disabled);
    }
    else
    {
        // Environment is OFF — all services effectively disabled for this env
        var allServices = allMicroservices.Select(m => m.Name).ToList();
        await _apisix.UpdateConsumerLabelsAsync(consumerUsername, new List<string>(), allServices);
    }

    Log.Information(
        "Consumer labels updated: {Consumer}, envEnabled={EnvEnabled}, enabled={Enabled}",
        consumerUsername, isEnvEnabled, string.Join(",", enabled));
}
        }

      public async Task RegenerateSecretAsync(int keyId)
{
    var newKey    = "ak_" + Guid.NewGuid().ToString("N");
    var newSecret = "sk_" + Guid.NewGuid().ToString("N");

    var keyInfo          = await _repository.GetApiKeyById(keyId);
    var consumerUsername = $"{keyInfo.ApplicationId}_{keyInfo.Environment.Replace("-", "_").Replace(" ", "_")}";
    var newConsumerKey   = $"{consumerUsername}_{Guid.NewGuid().ToString("N")[..8]}";

    await _repository.UpdateApiKeyAndSecret(keyId, newKey, newSecret);
    await _repository.UpdateConsumerKey(keyId, newConsumerKey);
    await _apisix.RegisterConsumerAsync(consumerUsername, newKey, newSecret, newConsumerKey);

    // Get current enabled services for this app to include in new token
    var details         = await _repository.GetApplicationDetails(keyInfo.ApplicationId);
    var enabledServices = details.Microservices.Where(m => m.IsEnabled).Select(m => m.Name).ToList();

    var newToken = _tokenService.GeneratePermanentApiToken(
        appId:            keyInfo.ApplicationId,
        environment:      keyInfo.Environment,
        consumerUsername: newConsumerKey,
        enabledServices:  enabledServices);

    await _repository.SaveAccessToken(keyId, newToken);

    Log.Information("Regenerated. KeyId: {KeyId}, NewConsumerKey: {CK}", keyId, newConsumerKey);
}
         public async Task RevokeKeyAsync(int keyId)
        {
            Log.Warning("Revoking API Key: {KeyId}", keyId);
            var keyInfo = await _repository.GetApiKeyById(keyId);
            var consumerUsername = $"{keyInfo.ApplicationId}_{keyInfo.Environment.Replace("-", "_").Replace(" ", "_")}";

            await _repository.RevokeApiKey(keyId);
            await _apisix.DeleteConsumerAsync(consumerUsername);

            Log.Warning("APISix consumer deleted for KeyId: {KeyId}", keyId);
        }

        public async Task<IEnumerable<Microservice>> GetMicroservicesAsync()
        {
            return await _repository.GetMicroservicesAsync();
        }
        // In ApplicationService

    }
}