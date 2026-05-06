using System.Text.Json;
using Dapper;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using Npgsql;
using Serilog;

namespace MicroserviceHub.API.Application.Services
{
    /// <summary>
    /// Reads routes from APISix Admin API and syncs them into microserviceroutes.
    ///
    /// REQUIRED LABELS on every APISix route:
    ///   microserviceid  — integer matching microservices.id in DB  (no underscore)
    ///   method          — e.g. GET  or  POST/GET
    ///   endpoint        — human-readable path shown in UI
    ///   description     — optional
    /// </summary>
    public class RouteSyncService : IRouteSyncService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration     _config;
        private readonly string             _connectionString;

        public RouteSyncService(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory      = httpFactory;
            _config           = config;
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public async Task<RouteSyncResult> SyncAsync()
        {
            var result = new RouteSyncResult { SyncedAt = DateTime.UtcNow };

            try
            {
                var adminUrl = _config["Apisix:AdminUrl"]!;
                var apiKey   = _config["Apisix:AdminKey"]!;

                // ── 1. Fetch all routes from APISix ──────────────────────────
                var http = _httpFactory.CreateClient("apisix-admin");
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

                var resp = await http.GetAsync($"{adminUrl}/apisix/admin/routes");
                if (!resp.IsSuccessStatusCode)
                {
                    var err = await resp.Content.ReadAsStringAsync();
                    result.Success = false;
                    result.Error   = $"APISix GET /routes failed [{resp.StatusCode}]: {err}";
                    Log.Error("RouteSyncService: {Error}", result.Error);
                    return result;
                }

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("list", out var list))
                {
                    result.Success = true;
                    result.Error   = "No routes found in APISix yet.";
                    return result;
                }

                var activeRouteIds = new List<string>();
                await using var conn = new NpgsqlConnection(_connectionString);

                // ── 2. For each APISix route, read labels and upsert to DB ──
                foreach (var item in list.EnumerateArray())
                {
                    result.TotalRoutes++;

                    if (!item.TryGetProperty("value", out var value)) continue;

                    // APISix route ID — could be a long number or a custom string
                    var routeId   = value.TryGetProperty("id",   out var rid) ? rid.GetString() ?? "" : "";
                    var routeName = value.TryGetProperty("name", out var rn)  ? rn.GetString()  ?? "" : "";
                    var uri       = value.TryGetProperty("uri",  out var u)   ? u.GetString()   ?? "" : "";

                    // Use name as routeId if it's meaningful, else use the numeric ID
                    // This is what gets stored in DB and used for APISix whitelist PATCH calls
                    var effectiveRouteId = !string.IsNullOrEmpty(routeName) ? routeName : routeId;

                    if (!string.IsNullOrEmpty(routeId))
                        activeRouteIds.Add(effectiveRouteId);

                    // Labels are required
                    if (!value.TryGetProperty("labels", out var labels))
                    {
                        result.Skipped++;
                        result.SkipReasons.Add(
                            $"Route '{routeName}' (id:{routeId}) — no labels. " +
                            "Edit in APISix Dashboard → Labels tab → add microserviceid, method, endpoint.");
                        continue;
                    }

                    // Support both "microserviceid" and "microservice_id" label keys
                    string? msIdStr = null;
                    if (labels.TryGetProperty("microserviceid",  out var msId1)) msIdStr = msId1.GetString();
                    else if (labels.TryGetProperty("microservice_id", out var msId2)) msIdStr = msId2.GetString();

                    if (string.IsNullOrEmpty(msIdStr) || !int.TryParse(msIdStr, out var microserviceId))
                    {
                        result.Skipped++;
                        result.SkipReasons.Add(
                            $"Route '{routeName}' — missing 'microserviceid' label. " +
                            "Add it in APISix Dashboard with value = microservices.id from your DB.");
                        continue;
                    }

                    // Read method label — support "POST/GET" format, take first one for display
                    var methodRaw = labels.TryGetProperty("method", out var m)
                        ? m.GetString() ?? "GET" : "GET";
                    var method = methodRaw.Split('/')[0].Trim().ToUpper();

                    // endpoint label — what's shown in UI as the route path
                    var endpoint = labels.TryGetProperty("endpoint", out var ep)
                        ? ep.GetString() ?? uri : uri;

                    var description = labels.TryGetProperty("description", out var desc)
                        ? desc.GetString()?.Replace("_", " ") : null;

                    // Verify microservice exists in DB
                    var msExists = await conn.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM microservices WHERE id = @Id AND isactive = TRUE",
                        new { Id = microserviceId });

                    if (msExists == 0)
                    {
                        result.Skipped++;
                        result.SkipReasons.Add(
                            $"Route '{routeName}' — microserviceid={microserviceId} not found " +
                            "in microservices table. Check your DB.");
                        continue;
                    }

                    var existing = await conn.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM microserviceroutes WHERE routeid = @RouteId",
                        new { RouteId = effectiveRouteId });

                    await conn.ExecuteAsync(@"
                        INSERT INTO microserviceroutes
                            (microserviceid, routeid, method, path, description, isactive, createdat)
                        VALUES
                            (@MsId, @RouteId, @Method, @Path, @Description, TRUE, NOW())
                        ON CONFLICT (microserviceid, routeid)
                        DO UPDATE SET
                            method      = @Method,
                            path        = @Path,
                            description = @Description,
                            isactive    = TRUE",
                        new
                        {
                            MsId        = microserviceId,
                            RouteId     = effectiveRouteId,
                            Method      = method,
                            Path        = endpoint,
                            Description = description
                        });

                    result.Synced++;

                    if (existing == 0)
                        result.Added.Add($"{method} {endpoint} → ms_id={microserviceId} (routeId: {effectiveRouteId})");
                    else
                        result.Updated.Add($"{method} {endpoint} → ms_id={microserviceId} (routeId: {effectiveRouteId})");

                    Log.Information(
                        "RouteSyncService: {Status} route '{RouteId}' → microservice_id={MsId} path={Path}",
                        existing == 0 ? "ADDED" : "UPDATED", effectiveRouteId, microserviceId, endpoint);
                }

                // ── 3. Mark routes removed from APISix as inactive ───────────
                // Uses Npgsql-compatible array operator instead of IN clause
                if (activeRouteIds.Count > 0)
                {
                    await conn.ExecuteAsync(
                        "UPDATE microserviceroutes SET isactive = FALSE WHERE NOT (routeid = ANY(@Ids))",
                        new { Ids = activeRouteIds.ToArray() });
                }
                else
                {
                    await conn.ExecuteAsync("UPDATE microserviceroutes SET isactive = FALSE");
                }

                result.Success = true;
                Log.Information(
                    "RouteSyncService: Sync complete. Total={Total} Synced={Synced} Skipped={Skipped}",
                    result.TotalRoutes, result.Synced, result.Skipped);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error   = ex.Message;
                Log.Error(ex, "RouteSyncService: Sync failed");
            }

            return result;
        }
    }
}