using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Utilities;
using Serilog;

namespace MicroserviceHub.API.Application.Services
{
    public class OAuthService : IOAuthService
    {
        private readonly IApplicationRepository _repository;
        private readonly OAuthTokenService      _tokenService;
        private readonly IConfiguration         _config;

        public OAuthService(
            IApplicationRepository repository,
            OAuthTokenService      tokenService,
            IConfiguration         config)
        {
            _repository   = repository;
            _tokenService = tokenService;
            _config       = config;
        }

        public async Task<TokenResponse> IssueApiTokenAsync(ClientCredentialsRequest request)
        {
            if (request.GrantType != "client_credentials")
                throw new InvalidOperationException("Unsupported grant_type. Use 'client_credentials'.");

            if (string.IsNullOrWhiteSpace(request.ClientId) ||
                string.IsNullOrWhiteSpace(request.ClientSecret))
                throw new UnauthorizedAccessException("client_id and client_secret are required.");

            // Look up the AppKey in the database
            var keyRecord = await _repository.GetApiKeyByAppKey(request.ClientId);

            if (keyRecord == null)
            {
                Log.Warning("OAuth token request — AppKey not found: {ClientId}", request.ClientId);
                throw new UnauthorizedAccessException("Invalid client credentials.");
            }

            // Verify the AppSecret matches
            if (keyRecord.AppSecretHash != request.ClientSecret)
            {
                Log.Warning("OAuth token request — AppSecret mismatch for AppKey: {ClientId}", request.ClientId);
                throw new UnauthorizedAccessException("Invalid client credentials.");
            }

            // Check the key is still active
            if (!keyRecord.IsActive || !keyRecord.IsEnvironmentEnabled)
            {
                Log.Warning("OAuth token request — key is revoked or disabled: {ClientId}", request.ClientId);
                throw new UnauthorizedAccessException("This key has been revoked or disabled.");
            }

            // Fetch enabled microservices for this application
            var details = await _repository.GetApplicationDetails(keyRecord.ApplicationId);
            var enabledServices = details.Microservices
                .Where(m => m.IsEnabled)
                .Select(m => m.Name)
                .ToList();

            var environment      = keyRecord.Environment;
            var consumerUsername = $"{keyRecord.ApplicationId}_{environment.Replace("-", "_").Replace(" ", "_")}";

            var token = _tokenService.GenerateApiToken(
                appId:           keyRecord.ApplicationId,
                environment:     environment,
                consumerUsername: consumerUsername,
                enabledServices: enabledServices);

            var expiryMinutes = _config.GetValue<int>("OAuth:ApiTokenExpiryMinutes", 60);

            Log.Information(
                "API token issued for AppId: {AppId}, Env: {Env}, Services: {Services}",
                keyRecord.ApplicationId, environment, string.Join(",", enabledServices));

            return new TokenResponse
            {
                AccessToken = token,
                TokenType   = "Bearer",
                ExpiresIn   = expiryMinutes * 60,
                Scope       = string.Join(" ", enabledServices)
            };
        }
    }
}