using System.Text;
using System.Text.Json;

namespace MicroserviceHub.API.Infrastructure.ExternalServices
{
    public class ApisixService
    {
        private readonly HttpClient _http;
        private readonly string _adminUrl;

        public ApisixService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _adminUrl = config["Apisix:AdminUrl"]!;
            _http.DefaultRequestHeaders.Add("X-API-KEY", config["Apisix:AdminKey"]!);
        }

        public async Task RegisterConsumerAsync(string username, string appKey)
        {
            // APISix plugin name is "key-auth" with a hyphen.
            // C# anonymous objects cannot have hyphens in property names,
            // so we build the JSON manually as a string to get the exact format.
            var body = $@"{{
                ""username"": ""{username}"",
                ""plugins"": {{
                    ""key-auth"": {{
                        ""key"": ""{appKey}""
                    }}
                }}
            }}";

            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PutAsync(
                $"{_adminUrl}/apisix/admin/consumers/{username}", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"APISix consumer register failed [{response.StatusCode}]: {error}");
            }
        }

        public async Task UpdateConsumerKeyAsync(string username, string newAppKey)
        {
            // Same structure as register — PUT is idempotent
            await RegisterConsumerAsync(username, newAppKey);
        }

        public async Task DeleteConsumerAsync(string username)
        {
            var response = await _http.DeleteAsync(
                $"{_adminUrl}/apisix/admin/consumers/{username}");

            // 404 means already gone — treat as success
            if (!response.IsSuccessStatusCode &&
                response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"APISix consumer delete failed [{response.StatusCode}]: {error}");
            }
        }
        // Called when admin enables/disables a microservice for an application.
// routeId = the APISix route ID (e.g. "iplookup")
// consumerUsername = e.g. "16_Development"
// allow = true to add to allowlist, false to remove
public async Task UpdateRouteAllowlistAsync(string routeId, string consumerUsername, bool allow)
{
    // Step 1 — GET the current route so we have the full config
    var getResponse = await _http.GetAsync(
        $"{_adminUrl}/apisix/admin/routes/{routeId}");

    if (!getResponse.IsSuccessStatusCode)
    {
        var err = await getResponse.Content.ReadAsStringAsync();
        throw new Exception($"APISix get route failed [{getResponse.StatusCode}]: {err}");
    }

    var routeJson = await getResponse.Content.ReadAsStringAsync();

    // Step 2 — Parse the current allowlist from the response
    // Response structure: { "value": { "plugins": { "consumer-restriction": { "allowlist": [...] } } } }
    var allowlist = new List<string>();
    using var doc = JsonDocument.Parse(routeJson);

    if (doc.RootElement.TryGetProperty("value", out var val) &&
        val.TryGetProperty("plugins", out var plugins) &&
        plugins.TryGetProperty("consumer-restriction", out var cr) &&
        cr.TryGetProperty("whitelist", out var al))
    {
        foreach (var item in al.EnumerateArray())
        {
            var entry = item.GetString();
            if (entry != null) allowlist.Add(entry);
        }
    }

    // Step 3 — Add or remove the consumer
    if (allow && !allowlist.Contains(consumerUsername))
        allowlist.Add(consumerUsername);
    else if (!allow)
        allowlist.Remove(consumerUsername);

    // Step 4 — Build the allowlist JSON array as a string
    var allowlistJson = "[" + string.Join(",", allowlist.Select(x => $"\"{x}\"")) + "]";

    // Step 5 — PATCH only the consumer-restriction plugin back
    var patchBody = $@"{{
    ""plugins"": {{
        ""consumer-restriction"": {{
            ""whitelist"": {allowlistJson}
        }}
    }}
}}";


    var content = new StringContent(patchBody, Encoding.UTF8, "application/json");
    var patchReq = new HttpRequestMessage(HttpMethod.Patch,
        $"{_adminUrl}/apisix/admin/routes/{routeId}") { Content = content };

    var patchResp = await _http.SendAsync(patchReq);

    if (!patchResp.IsSuccessStatusCode)
    {
        var err = await patchResp.Content.ReadAsStringAsync();
        throw new Exception($"APISix update allowlist failed [{patchResp.StatusCode}]: {err}");
    }
}
    }
}