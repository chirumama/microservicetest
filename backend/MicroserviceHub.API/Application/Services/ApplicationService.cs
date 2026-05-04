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

                    var permanentToken = _tokenService.GeneratePermanentApiToken(
                        appId:            appId,
                        environment:      env,
                        consumerUsername: consumerUsername,
                        enabledServices:  new List<string>());

                    var keyRecord = await _repository.GetApiKeyByAppKey(appKey);
                    if (keyRecord != null)
                        await _repository.SaveAccessToken(keyRecord.Id, permanentToken);
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

            var allMicroservices = (await _repository.GetMicroservicesAsync()).ToList();

            var enabled  = request.Microservices
                .Where(m => m.IsEnabled)
                .Select(m => allMicroservices.FirstOrDefault(x => x.Id == m.Id)?.Name)
                .Where(n => n != null).Select(n => n!).ToList();

            var disabled = request.Microservices
                .Where(m => !m.IsEnabled)
                .Select(m => allMicroservices.FirstOrDefault(x => x.Id == m.Id)?.Name)
                .Where(n => n != null).Select(n => n!).ToList();

            // Step 1 — update SQL
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

            // Step 2 — when a microservice is enabled, seed all its routes as enabled
            //           if no applicationroutes rows exist yet
            foreach (var micro in request.Microservices.Where(m => m.IsEnabled))
            {
                var routes = await _repository.GetMicroserviceRoutesAsync(micro.Id);
                foreach (var route in routes)
                {
                    // Only insert if not already present (don't overwrite user's per-route choices)
                    await _repository.UpsertApplicationRouteAsync(appId, micro.Id, route.RouteId, true);
                }
            }

            var environments = new[] { "Development", "Pre-Production", "Production" };

            // Step 3 — sync APISix per-route whitelists
            await SyncAllRouteWhitelistsAsync(appId, request, allMicroservices, environments);

            // Step 4 — update consumer labels + regenerate tokens
            var details = await _repository.GetApplicationDetails(appId);

            foreach (var env in environments)
            {
                var consumerUsername = $"{appId}_{env.Replace("-", "_").Replace(" ", "_")}";
                var envRequest       = request.Environments.FirstOrDefault(e => e.Name == env);
                var isEnvEnabled     = envRequest?.IsEnabled ?? false;

                var servicesForToken = isEnvEnabled ? enabled : new List<string>();

                var newToken = _tokenService.GeneratePermanentApiToken(
                    appId:            appId,
                    environment:      env,
                    consumerUsername: consumerUsername,
                    enabledServices:  servicesForToken);

                var envRecord = details.Environments.FirstOrDefault(e => e.Environment == env);
                if (envRecord != null)
                    await _repository.SaveAccessToken(envRecord.Id, newToken);

                if (isEnvEnabled)
                    await _apisix.UpdateConsumerLabelsAsync(consumerUsername, enabled, disabled);
                else
                    await _apisix.UpdateConsumerLabelsAsync(consumerUsername, new List<string>(), allMicroservices.Select(m => m.Name).ToList());
            }
        }

        // ── Route-level access control ─────────────────────────────────────────

        public async Task<MicroserviceWithRoutesDto> GetMicroserviceRoutesAsync(int appId, int microserviceId)
        {
            var ms     = (await _repository.GetMicroservicesAsync()).FirstOrDefault(m => m.Id == microserviceId);
            var details = await _repository.GetApplicationDetails(appId);
            var msState = details.Microservices.FirstOrDefault(m => m.Id == microserviceId);
            var routes  = await _repository.GetRoutesForAppAsync(appId, microserviceId);

            return new MicroserviceWithRoutesDto
            {
                MicroserviceId   = microserviceId,
                MicroserviceName = ms?.Name ?? string.Empty,
                IsEnabled        = msState?.IsEnabled ?? false,
                Routes           = routes
            };
        }

        public async Task UpdateRouteAccessAsync(int appId, int microserviceId, UpdateRouteAccessRequest request)
        {
            // Save per-route state to DB
            foreach (var route in request.Routes)
                await _repository.UpsertApplicationRouteAsync(appId, microserviceId, route.RouteId, route.IsEnabled);

            Log.Information("Route access updated for AppId: {AppId}, MsId: {MsId}", appId, microserviceId);

            // Sync all environment consumers on each route
            var environments = new[] { "Development", "Pre-Production", "Production" };
            var details      = await _repository.GetApplicationDetails(appId);

            foreach (var route in request.Routes)
            {
                // Get all apps/environments that have this route enabled
                // Rebuild whitelist: for each environment consumer of this app, add or remove
                await SyncSingleRouteWhitelistAsync(appId, microserviceId, route.RouteId, environments, details);
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────

        /// <summary>
        /// After a settings update, syncs every route in every microservice for all 3 environments.
        /// A consumer is added to a route's whitelist only if:
        ///   - the microservice is enabled for this app
        ///   - the environment is enabled
        ///   - the specific route is enabled for this app
        /// </summary>
        private async Task SyncAllRouteWhitelistsAsync(
            int appId,
            UpdateApplicationSettingsRequest request,
            List<Microservice> allMicroservices,
            string[] environments)
        {
            var routeMap = _config.GetSection("ApisixRoutes")
                .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();

            foreach (var micro in allMicroservices)
            {
                var msRequest  = request.Microservices.FirstOrDefault(m => m.Id == micro.Id);
                var msEnabled  = msRequest?.IsEnabled ?? false;

                var routes = await _repository.GetMicroserviceRoutesAsync(micro.Id);

                foreach (var route in routes)
                {
                    // Build whitelist: consumers (env-specific) that are allowed on this route
                    var allowedConsumers = new List<string>();

                    if (msEnabled)
                    {
                        foreach (var env in environments)
                        {
                            var envRequest   = request.Environments.FirstOrDefault(e => e.Name == env);
                            var isEnvEnabled = envRequest?.IsEnabled ?? false;
                            if (!isEnvEnabled) continue;

                            // Check per-route state (defaults to enabled)
                            var appRoutes    = await _repository.GetRoutesForAppAsync(appId, micro.Id);
                            var routeEnabled = appRoutes.FirstOrDefault(r => r.RouteId == route.RouteId)?.IsEnabled ?? true;
                            if (!routeEnabled) continue;

                            var consumerUsername = $"{appId}_{env.Replace("-", "_").Replace(" ", "_")}";
                            allowedConsumers.Add(consumerUsername);
                        }
                    }

                    try
                    {
                        await _apisix.SyncRouteWhitelistAsync(route.RouteId, allowedConsumers);
                        Log.Information("Route whitelist synced: {RouteId} — allowed: [{Consumers}]",
                            route.RouteId, string.Join(", ", allowedConsumers));
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Failed to sync APISix route {RouteId}: {Msg}", route.RouteId, ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Syncs the whitelist for a single route across all environments of this app.
        /// </summary>
        private async Task SyncSingleRouteWhitelistAsync(
            int appId, int microserviceId, string routeId,
            string[] environments, GetApplicationDetailsResponse details)
        {
            var allowedConsumers = new List<string>();

            var msState = details.Microservices.FirstOrDefault(m => m.Id == microserviceId);
            if (msState?.IsEnabled == true)
            {
                foreach (var env in environments)
                {
                    var envRecord    = details.Environments.FirstOrDefault(e => e.Environment == env);
                    var isEnvEnabled = envRecord?.IsEnabled ?? false;
                    if (!isEnvEnabled) continue;

                    var appRoutes    = await _repository.GetRoutesForAppAsync(appId, microserviceId);
                    var routeEnabled = appRoutes.FirstOrDefault(r => r.RouteId == routeId)?.IsEnabled ?? true;
                    if (!routeEnabled) continue;

                    var consumerUsername = $"{appId}_{env.Replace("-", "_").Replace(" ", "_")}";
                    allowedConsumers.Add(consumerUsername);
                }
            }

            try
            {
                await _apisix.SyncRouteWhitelistAsync(routeId, allowedConsumers);
                Log.Information("Single route whitelist synced: {RouteId} — [{Consumers}]",
                    routeId, string.Join(", ", allowedConsumers));
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to sync APISix route {RouteId}: {Msg}", routeId, ex.Message);
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
            var keyInfo          = await _repository.GetApiKeyById(keyId);
            var consumerUsername = $"{keyInfo.ApplicationId}_{keyInfo.Environment.Replace("-", "_").Replace(" ", "_")}";

            await _repository.RevokeApiKey(keyId);
            await _apisix.DeleteConsumerAsync(consumerUsername);
            Log.Warning("APISix consumer deleted for KeyId: {KeyId}", keyId);
        }

        public async Task<IEnumerable<Microservice>> GetMicroservicesAsync()
        {
            return await _repository.GetMicroservicesAsync();
        }
    }
}