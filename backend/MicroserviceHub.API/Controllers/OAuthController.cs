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

        /// <summary>
        /// POST /v1.0.1/oauth/token
        /// Developer's app sends AppKey + AppSecret, gets back a JWT access token.
        /// APISix validates this token on every microservice call.
        /// </summary>
        [HttpPost("token")]
        public async Task<IActionResult> Token([FromBody] ClientCredentialsRequest request)
        {
            var result = await _oauthService.IssueApiTokenAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// GET /v1.0.1/oauth/.well-known/jwks.json
        /// APISix fetches this to get Hub's public key for token verification.
        /// This endpoint is public — no auth required.
        /// </summary>
        [HttpGet(".well-known/jwks.json")]
        public IActionResult Jwks()
        {
            var jwk = _keys.GetPublicJwk();
            return Ok(new JsonWebKeySet { Keys = { jwk } });
        }
    }
}