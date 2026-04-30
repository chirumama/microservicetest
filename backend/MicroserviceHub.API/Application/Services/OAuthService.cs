using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Utilities;
using Microsoft.IdentityModel.Tokens;
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

            // Return the stored permanent token
            var storedToken = await _repository.GetAccessToken(keyRecord.Id);

            if (storedToken == null)
            {
                // Fallback: generate and store if missing (older records before this feature)
                Log.Warning("No stored token found for KeyId {Id} — generating fallback", keyRecord.Id);

                var details = await _repository.GetApplicationDetails(keyRecord.ApplicationId);
                var enabledServices = details.Microservices
                    .Where(m => m.IsEnabled)
                    .Select(m => m.Name)
                    .ToList();

                var consumerUsername = $"{keyRecord.ApplicationId}_{keyRecord.Environment.Replace("-", "_").Replace(" ", "_")}";

                storedToken = _tokenService.GeneratePermanentApiToken(
                    keyRecord.ApplicationId,
                    keyRecord.Environment,
                    consumerUsername,
                    enabledServices);

                await _repository.SaveAccessToken(keyRecord.Id, storedToken);
            }

            var scope = GetServicesFromToken(storedToken);

            Log.Information("Permanent token returned for AppId: {Id}, Env: {Env}",
                keyRecord.ApplicationId, keyRecord.Environment);

            return new TokenResponse
            {
                AccessToken = storedToken,
                TokenType   = "Bearer",
                ExpiresIn   = 0,
                Scope       = string.Join(" ", scope)
            };
        }

        private static List<string> GetServicesFromToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt     = handler.ReadJwtToken(token);
                var services = jwt.Claims.FirstOrDefault(c => c.Type == "services")?.Value ?? "";
                return services.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();
            }
            catch { return new List<string>(); }
        }
    }
}