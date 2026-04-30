using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Infrastructure.ExternalServices;
using MicroserviceHub.API.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MicroserviceHub.API.Controllers
{
    [ApiController]
    [Route("v1.0.1/oauth")]
    public class OAuthController : ControllerBase
    {
        private readonly IOAuthService  _oauthService;
        private readonly RsaKeyProvider _keys;
        private readonly ApisixService  _apisix;

        public OAuthController(
            IOAuthService  oauthService,
            RsaKeyProvider keys,
            ApisixService  apisix)
        {
            _oauthService = oauthService;
            _keys         = keys;
            _apisix       = apisix;
        }

        // Developer's app → POST with AppKey+AppSecret → gets JWT access token
        [HttpPost("token")]
        public async Task<IActionResult> Token([FromBody] ClientCredentialsRequest request)
        {
            var result = await _oauthService.IssueApiTokenAsync(request);
            return Ok(result);
        }

        // APISix fetches this to get the public key for token verification
        [HttpGet(".well-known/jwks.json")]
        public IActionResult Jwks()
        {
            var jwk = _keys.GetPublicJwk();
            return Ok(new JsonWebKeySet { Keys = { jwk } });
        }

        // Helper — get public key as PEM for pasting into APISix consumer config
        [HttpGet(".well-known/public-key.pem")]
        public IActionResult PublicKeyPem()
        {
            return Content(_keys.GetPemPublicKey(), "text/plain");
        }

        // Admin only — registers upstream + route in APISix for a new microservice
        // Call once per microservice when adding it to the system
       [Authorize]
[HttpPost("setup-route")]
public async Task<IActionResult> SetupRoute([FromBody] SetupRouteRequest request)
{
    await _apisix.RegisterUpstreamAsync(request.UpstreamId, request.Host, request.Port);
    await _apisix.RegisterRouteAsync(
        request.RouteId,
        request.UriPattern,
        request.UpstreamId,
        request.RewriteFrom,
        request.RewriteTo);
    return Ok(new { message = $"Route {request.RouteId} registered successfully" });
}
    }
}