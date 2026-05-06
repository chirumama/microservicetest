using System.IdentityModel.Tokens.Jwt;
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

            var keyRecord = await _repository.GetApiKeyByAppKey(request.ClientId);

            if (keyRecord == null)
            {
                Log.Warning("OAuth token request — AppKey not found: {ClientId}", request.ClientId);
                throw new UnauthorizedAccessException("Invalid client credentials.");
            }

            if (keyRecord.AppSecretHash != request.ClientSecret)
            {
                Log.Warning("OAuth token request — AppSecret mismatch: {ClientId}", request.ClientId);
                throw new UnauthorizedAccessException("Invalid client credentials.");
            }

            if (!keyRecord.IsActive || !keyRecord.IsEnvironmentEnabled)
            {
                Log.Warning("OAuth token request — key revoked or disabled: {ClientId}", request.ClientId);
                throw new UnauthorizedAccessException("This key has been revoked or disabled.");
            }

            // Get enabled services and consumer key from DB
            var details = await _repository.GetApplicationDetails(keyRecord.ApplicationId);
            var enabledServices = details.Microservices
                .Where(m => m.IsEnabled)
                .Select(m => m.Name)
                .ToList();

            // Read the stored consumer key — set during RegisterConsumer / Regenerate
            var storedConsumerKey = await _repository.GetConsumerKey(keyRecord.Id);
            var consumerKey = !string.IsNullOrEmpty(storedConsumerKey)
                ? storedConsumerKey
                : $"{keyRecord.ApplicationId}_{keyRecord.Environment.Replace("-", "_").Replace(" ", "_")}";

            // Always generate a FRESH token — new jti every call so APISix never rejects it
            var freshToken = _tokenService.GeneratePermanentApiToken(
                keyRecord.ApplicationId,
                keyRecord.Environment,
                consumerKey,
                enabledServices);

            Log.Information("Fresh permanent token issued for AppId: {Id}, Env: {Env}, Consumer: {CK}",
                keyRecord.ApplicationId, keyRecord.Environment, consumerKey);

            return new TokenResponse
            {
                AccessToken = freshToken,
                TokenType   = "Bearer",
                ExpiresIn   = _config.GetValue<int>("OAuth:ApiTokenExpiryMinutes", 60) * 60,
                Scope       = string.Join(" ", enabledServices)
            };
        }
    }
}