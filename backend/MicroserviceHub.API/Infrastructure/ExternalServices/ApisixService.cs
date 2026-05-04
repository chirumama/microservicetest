using System.Text;
using System.Text.Json;
using MicroserviceHub.API.Utilities; 

namespace MicroserviceHub.API.Infrastructure.ExternalServices
{
    public class ApisixService
{
    private readonly HttpClient     _http;
    private readonly string         _adminUrl;
    private readonly RsaKeyProvider _rsaKeyProvider;

    public ApisixService(HttpClient http, IConfiguration config, RsaKeyProvider rsaKeyProvider)
    {
        _http           = http;
        _adminUrl       = config["Apisix:AdminUrl"]!;
        _rsaKeyProvider = rsaKeyProvider;
        _http.DefaultRequestHeaders.Add("X-API-KEY", config["Apisix:AdminKey"]!);
    }

        // Registers consumer with basic-auth.
        // APISix stores the password hashed internally — developer uses the plain sk_... value.
        // desc field stores AppKey + AppSecret in plain text for visibility on APISix dashboard.
        public async Task RegisterConsumerAsync(
    string username,
    string appKey,
    string appSecret,
    string? consumerKey = null)
{
    var jwtKey       = consumerKey ?? username;
    var pemPublicKey = _rsaKeyProvider.GetPemPublicKey();
    var pemEscaped   = pemPublicKey.Replace("\r", "").Replace("\n", "\\n");

    var body = $@"{{
        ""username"": ""{username}"",
        ""desc"": ""AppKey: {appKey} | AppSecret: {appSecret}"",
        ""plugins"": {{
            ""jwt-auth"": {{
                ""key"":        ""{jwtKey}"",
                ""algorithm"":  ""RS256"",
                ""public_key"": ""{pemEscaped}"",
                ""exp"":        315360000 
            }}
        }}
    }}";

    var content  = new StringContent(body, Encoding.UTF8, "application/json");
    var response = await _http.PutAsync(
        $"{_adminUrl}/apisix/admin/consumers/{username}", content);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"APISix consumer register failed [{response.StatusCode}]: {error}");
    }
}

        // Called after admin updates microservice permissions.
        // Updates the services_enabled label on the consumer so APISix dashboard shows current state.
        public async Task UpdateConsumerLabelsAsync(string username, List<string> enabledServices, List<string> disabledServices)
{
    var enabledStr  = enabledServices.Count  > 0 ? string.Join(",", enabledServices.Select(s => s.Replace(" ", "_")))  : "none";
var disabledStr = disabledServices.Count > 0 ? string.Join(",", disabledServices.Select(s => s.Replace(" ", "_"))) : "none";

    // GET current consumer first so we preserve all existing plugin config
    var getResp = await _http.GetAsync($"{_adminUrl}/apisix/admin/consumers/{username}");
    if (!getResp.IsSuccessStatusCode) return; // consumer revoked or missing — skip

    var json = await getResp.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(json);
    var value = doc.RootElement.GetProperty("value");

    // Read existing desc and public key so we don't lose them
    var desc      = value.TryGetProperty("desc", out var d)   ? d.GetString() ?? "" : "";
    var publicKey = "";
    var keyName   = username;
    var algo      = "RS256";

    if (value.TryGetProperty("plugins", out var pl) &&
        pl.TryGetProperty("jwt-auth", out var ja))
    {
        publicKey = ja.TryGetProperty("public_key", out var pk) ? pk.GetString() ?? "" : "";
        keyName   = ja.TryGetProperty("key",        out var k)  ? k.GetString()  ?? username : username;
        algo      = ja.TryGetProperty("algorithm",  out var al) ? al.GetString() ?? "RS256"  : "RS256";
    }

    // Escape the PEM key for JSON embedding
    var pemEscaped = publicKey.Replace("\r", "").Replace("\n", "\\n");

    var body = $@"{{
        ""username"": ""{username}"",
        ""desc"": ""{desc}"",
        ""labels"": {{
            ""services_enabled"":  ""{enabledStr}"",
            ""services_disabled"": ""{disabledStr}""
        }},
        ""plugins"": {{
            ""jwt-auth"": {{
                ""key"":        ""{keyName}"",
                ""algorithm"":  ""{algo}"",
                ""public_key"": ""{pemEscaped}""
            }}
        }}
    }}";

    var content  = new StringContent(body, Encoding.UTF8, "application/json");
    var response = await _http.PutAsync(
        $"{_adminUrl}/apisix/admin/consumers/{username}", content);

    if (!response.IsSuccessStatusCode)
    {
        var err = await response.Content.ReadAsStringAsync();
        throw new Exception($"APISix label update failed [{response.StatusCode}]: {err}");
    }
}

        // Called on regenerate — replaces credentials in etcd.
        // desc is updated so the APISix dashboard reflects the new AppKey + AppSecret.
        public async Task UpdateConsumerKeyAsync(string username, string newAppKey, string newAppSecret)
        {
            await RegisterConsumerAsync(username, newAppKey, newAppSecret);
        }

        // Called on revoke — deletes consumer from etcd entirely.
        // Any request with the old AppKey returns 401 instantly.
        public async Task DeleteConsumerAsync(string username)
        {
            var response = await _http.DeleteAsync(
                $"{_adminUrl}/apisix/admin/consumers/{username}");

            // 404 = already gone — treat as success
            if (!response.IsSuccessStatusCode &&
                response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"APISix consumer delete failed [{response.StatusCode}]: {error}");
            }
        }

        // Called when admin enables/disables a microservice for an application.
        // Adds or removes the consumer username from the route's whitelist in etcd.
        public async Task UpdateRouteAllowlistAsync(string routeId, string consumerUsername, bool allow)
{
    var getResponse = await _http.GetAsync(
        $"{_adminUrl}/apisix/admin/routes/{routeId}");

    if (!getResponse.IsSuccessStatusCode)
    {
        var err = await getResponse.Content.ReadAsStringAsync();
        throw new Exception($"APISix get route failed [{getResponse.StatusCode}]: {err}");
    }

    var routeJson = await getResponse.Content.ReadAsStringAsync();
    var whitelist = new List<string>();

    using var doc = JsonDocument.Parse(routeJson);
    if (doc.RootElement.TryGetProperty("value", out var val) &&
        val.TryGetProperty("plugins", out var plugins) &&
        plugins.TryGetProperty("consumer-restriction", out var cr) &&
        cr.TryGetProperty("whitelist", out var wl))
    {
        foreach (var item in wl.EnumerateArray())
        {
            var entry = item.GetString();
            if (entry != null) whitelist.Add(entry);
        }
    }

    if (allow && !whitelist.Contains(consumerUsername))
        whitelist.Add(consumerUsername);
    else if (!allow)
        whitelist.Remove(consumerUsername);

    string patchBody;
   if (whitelist.Count == 0)
{
    patchBody = @"{ ""plugins"": { ""consumer-restriction"": null } }";
}
else
{
    var whitelistJson = "[" + string.Join(",", whitelist.Select(x => $"\"{x}\"")) + "]";
    patchBody = $@"{{
        ""plugins"": {{
            ""consumer-restriction"": {{
                ""type"": ""consumer_name"",
                ""whitelist"": {whitelistJson},
                ""rejected_code"": 403
            }}
        }}
    }}";
}

    var patchContent = new StringContent(patchBody, Encoding.UTF8, "application/json");
    var patchReq     = new HttpRequestMessage(HttpMethod.Patch,
        $"{_adminUrl}/apisix/admin/routes/{routeId}") { Content = patchContent };

    var patchResp = await _http.SendAsync(patchReq);
    if (!patchResp.IsSuccessStatusCode)
    {
        var err = await patchResp.Content.ReadAsStringAsync();
        throw new Exception($"APISix update whitelist failed [{patchResp.StatusCode}]: {err}");
    }
}
public async Task RegisterRouteAsync(
    string routeId,
    string uriPattern,
    string upstreamId,
    string? rewriteFrom = null,
    string? rewriteTo   = null)
{
    var proxyRewritePlugin = "";
    if (!string.IsNullOrEmpty(rewriteFrom) && !string.IsNullOrEmpty(rewriteTo))
    {
        proxyRewritePlugin = $@",
        ""proxy-rewrite"": {{
            ""regex_uri"": [""{rewriteFrom}"", ""{rewriteTo}""]
        }}";
    }

    var body = $@"{{
        ""uri"":         ""{uriPattern}"",
        ""methods"":     [""GET"", ""POST"", ""PUT"", ""PATCH"", ""DELETE""],
        ""upstream_id"": ""{upstreamId}"",
        ""plugins"": {{
            ""jwt-auth"": {{
                ""hide_credentials"": false
            }}{proxyRewritePlugin}
        }}
    }}";

    var content  = new StringContent(body, Encoding.UTF8, "application/json");
    var response = await _http.PutAsync(
        $"{_adminUrl}/apisix/admin/routes/{routeId}", content);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"APISix route register failed [{response.StatusCode}]: {error}");
    }
}

public async Task RegisterUpstreamAsync(string upstreamId, string host, int port)
{
    var body = $@"{{
        ""id"":   ""{upstreamId}"",
        ""type"": ""roundrobin"",
        ""nodes"": {{
            ""{host}:{port}"": 1
        }}
    }}";

    var content  = new StringContent(body, Encoding.UTF8, "application/json");
    var response = await _http.PutAsync(
        $"{_adminUrl}/apisix/admin/upstreams/{upstreamId}", content);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        throw new Exception($"APISix upstream register failed [{response.StatusCode}]: {error}");
    }
}

        /// <summary>
        /// Syncs a specific APISix route's consumer-restriction whitelist.
        /// Consumers in allowedConsumers will be permitted; all others will get 403.
        /// </summary>
        public async Task SyncRouteWhitelistAsync(string routeId, List<string> allowedConsumers)
        {
            string patchBody;
            if (allowedConsumers.Count == 0)
            {
                patchBody = @"{
    ""plugins"": {
        ""consumer-restriction"": {
            ""type"": ""consumer_name"",
            ""whitelist"": [],
            ""rejected_code"": 403
        }
    }
}";
            }
            else
            {
                var whitelistJson = "[" + string.Join(",", allowedConsumers.Select(x => $"\"{x}\"")) + "]";
                patchBody = $@"{{
    ""plugins"": {{
        ""consumer-restriction"": {{
            ""type"": ""consumer_name"",
            ""whitelist"": {whitelistJson},
            ""rejected_code"": 403
        }}
    }}
}}";
            }

            var patchContent = new StringContent(patchBody, System.Text.Encoding.UTF8, "application/json");
            var patchReq     = new HttpRequestMessage(HttpMethod.Patch,
                $"{_adminUrl}/apisix/admin/routes/{routeId}") { Content = patchContent };

            var patchResp = await _http.SendAsync(patchReq);
            if (!patchResp.IsSuccessStatusCode)
            {
                var err = await patchResp.Content.ReadAsStringAsync();
                throw new Exception($"APISix route whitelist sync failed [{patchResp.StatusCode}]: {err}");
            }
        }

    }
}