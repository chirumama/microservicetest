using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace MicroserviceHub.API.Utilities
{
    public class RsaKeyProvider
    {
        private readonly RSA _rsa;
        public string KeyId { get; }

        public RsaKeyProvider(IConfiguration config)
{
    KeyId = config["OAuth:KeyId"] ?? "msh-rsa-key-1";

    var privateKey = config["OAuth:PrivateKeyBase64"]
        ?? throw new InvalidOperationException("OAuth:PrivateKeyBase64 is missing.");

    _rsa = RSA.Create();

    privateKey = privateKey.Trim();

    if (privateKey.StartsWith("<RSAKeyValue>"))
    {
        // XML format (your current config)
        _rsa.FromXmlString(privateKey);
    }
    else
    {
        // Base64 PKCS#1 (future support)
        _rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
    }
}

        // Used for signing tokens
        public RsaSecurityKey GetPrivateKey()
            => new RsaSecurityKey(_rsa) { KeyId = KeyId };

        // Used for the JWKS endpoint — public parameters only
        public JsonWebKey GetPublicJwk()
        {
            var parameters = _rsa.ExportParameters(includePrivateParameters: false);
            return new JsonWebKey
            {
                Kty = "RSA",
                Use = "sig",
                Kid = KeyId,
                Alg = "RS256",
                N   = Base64UrlEncoder.Encode(parameters.Modulus!),
                E   = Base64UrlEncoder.Encode(parameters.Exponent!)
            };
        }
    }
}