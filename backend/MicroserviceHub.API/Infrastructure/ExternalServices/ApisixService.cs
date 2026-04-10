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

        // Registers consumer with basic-auth — validates BOTH AppKey and AppSecret
        public async Task RegisterConsumerAsync(string username, string appKey, string appSecret)
        {
            var body = $@"{{
                ""username"": ""{username}"",
                ""plugins"": {{
                    ""basic-auth"": {{
                        ""username"": ""{appKey}"",
                        ""password"": ""{appSecret}""
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

        public async Task UpdateConsumerKeyAsync(string username, string newAppKey, string newAppSecret)
        {
            await RegisterConsumerAsync(username, newAppKey, newAppSecret);
        }

        public async Task DeleteConsumerAsync(string username)
        {
            var response = await _http.DeleteAsync(
                $"{_adminUrl}/apisix/admin/consumers/{username}");

            if (!response.IsSuccessStatusCode &&
                response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"APISix consumer delete failed [{response.StatusCode}]: {error}");
            }
        }

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
                            ""whitelist"": {whitelistJson}
                        }}
                    }}
                }}";
            }

            var patchContent = new StringContent(patchBody, Encoding.UTF8, "application/json");
            var patchReq = new HttpRequestMessage(HttpMethod.Patch,
                $"{_adminUrl}/apisix/admin/routes/{routeId}") { Content = patchContent };

            var patchResp = await _http.SendAsync(patchReq);
            if (!patchResp.IsSuccessStatusCode)
            {
                var err = await patchResp.Content.ReadAsStringAsync();
                throw new Exception($"APISix update whitelist failed [{patchResp.StatusCode}]: {err}");
            }
        }
    }
}