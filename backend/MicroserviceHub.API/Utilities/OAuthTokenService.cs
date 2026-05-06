using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace MicroserviceHub.API.Utilities
{
    public class OAuthTokenService
    {
        private readonly RsaKeyProvider _keys;
        private readonly IConfiguration _config;

        public OAuthTokenService(RsaKeyProvider keys, IConfiguration config)
        {
            _keys   = keys;
            _config = config;
        }

        public string GenerateDashboardToken(int userId, int roleId, string role, string email)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,     userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub,   userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("roleId",                      roleId.ToString()),
                new Claim(ClaimTypes.Role,               role),
                new Claim("role",                        role),
                new Claim("type",                        "dashboard"),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
            };

            return BuildToken(
                claims,
                audience:      _config["OAuth:DashboardAudience"]!,
                expiryMinutes: _config.GetValue<int>("OAuth:DashboardTokenExpiryMinutes", 60));
        }

        public string GenerateApiToken(
            int          appId,
            string       environment,
            string       consumerUsername,
            List<string> enabledServices)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, $"app_{appId}"),
                new Claim("key",      consumerUsername),
                new Claim("appId",    appId.ToString()),
                new Claim("env",      environment),
                new Claim("consumer", consumerUsername),
                new Claim("services", string.Join(",", enabledServices)),
                new Claim("type",     "api_access"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            return BuildToken(
                claims,
                audience:      _config["OAuth:ApiAudience"]!,
                expiryMinutes: _config.GetValue<int>("OAuth:ApiTokenExpiryMinutes", 60));
        }

        // Permanent token — no expiry, only invalidated by admin action
        public string GeneratePermanentApiToken(
            int          appId,
            string       environment,
            string       consumerUsername,
            List<string> enabledServices)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, $"app_{appId}"),
                new Claim("key",      consumerUsername),
                new Claim("appId",    appId.ToString()),
                new Claim("env",      environment),
                new Claim("consumer", consumerUsername),
                new Claim("services", string.Join(",", enabledServices)),
                new Claim("type",     "api_access_permanent"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var signingKey  = _keys.GetPrivateKey();
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);
            var issuer      = _config["OAuth:Issuer"];
            var audience    = _config["OAuth:ApiAudience"]!;

            // No expires — permanent until admin action
            var token = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                notBefore:          DateTime.UtcNow,
                expires:            DateTime.UtcNow.AddYears(10),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string BuildToken(IEnumerable<Claim> claims, string audience, int expiryMinutes)
        {
            var credentials = new SigningCredentials(
                _keys.GetPrivateKey(), SecurityAlgorithms.RsaSha256);

            var token = new JwtSecurityToken(
                issuer:             _config["OAuth:Issuer"],
                audience:           audience,
                claims:             claims,
                notBefore:          DateTime.UtcNow,
                expires:            DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}