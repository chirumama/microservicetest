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
                _rsa.FromXmlString(privateKey);
            else
                _rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        }

        // Used for SIGNING tokens — private key required
        public RsaSecurityKey GetPrivateKey()
            => new RsaSecurityKey(_rsa) { KeyId = KeyId };

        // Used for VALIDATING tokens — public key only
        public RsaSecurityKey GetPublicKey()
        {
            var publicRsa = RSA.Create();
            publicRsa.ImportParameters(_rsa.ExportParameters(includePrivateParameters: false));
            return new RsaSecurityKey(publicRsa) { KeyId = KeyId };
        }

        // Used for JWKS endpoint — public parameters as JWK
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

        // Used for APISix consumer registration — PEM format
        public string GetPemPublicKey()
        {
            var pubKeyBytes = _rsa.ExportSubjectPublicKeyInfo();
            var b64 = Convert.ToBase64String(pubKeyBytes, Base64FormattingOptions.InsertLineBreaks);
            return $"-----BEGIN PUBLIC KEY-----\n{b64}\n-----END PUBLIC KEY-----";
        }
    }
}