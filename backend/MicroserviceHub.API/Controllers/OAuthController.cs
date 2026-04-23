using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Utilities;
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

        public OAuthController(IOAuthService oauthService, RsaKeyProvider keys)
        {
            _oauthService = oauthService;
            _keys         = keys;
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
    }
}