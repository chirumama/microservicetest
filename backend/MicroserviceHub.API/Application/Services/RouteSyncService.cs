using System.Text.Json;
using Dapper;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using Npgsql;
using Serilog;

namespace MicroserviceHub.API.Application.Services
{
    /// <summary>
    /// Reads routes from APISix Admin API and syncs them into the
    /// microserviceroutes table. 
    ///
    /// REQUIRED LABELS on every APISix route for sync to work:
    ///   microservice_id  — integer, must match microservices.id in DB
    ///   method           — HTTP method e.g. GET, POST
    ///   endpoint         — human-readable path e.g. /api/passport/verify
    ///   description      — short description (optional but recommended)
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

                await using var conn = new NpgsqlConnection(_connectionString);

                // ── 2. For each APISix route, read labels and upsert to DB ──
                foreach (var item in list.EnumerateArray())
                {
                    result.TotalRoutes++;

                    if (!item.TryGetProperty("value", out var value)) continue;

                    var routeId = value.TryGetProperty("id",   out var rid) ? rid.GetString() ?? "" : "";
                    var uri     = value.TryGetProperty("uri",  out var u)   ? u.GetString()   ?? "" : "";

                    // Labels are required — without them we can't map to a microservice
                    if (!value.TryGetProperty("labels", out var labels))
                    {
                        result.Skipped++;
                        result.SkipReasons.Add($"Route '{routeId}' skipped — no labels set");
                        continue;
                    }

                    // microservice_id label is mandatory
                    var msIdStr = labels.TryGetProperty("microservice_id", out var msId)
                        ? msId.GetString() : null;

                    if (string.IsNullOrEmpty(msIdStr) || !int.TryParse(msIdStr, out var microserviceId))
                    {
                        result.Skipped++;
                        result.SkipReasons.Add($"Route '{routeId}' skipped — missing or invalid 'microservice_id' label");
                        continue;
                    }

                    // method label — how the endpoint is accessed
                    var method = labels.TryGetProperty("method", out var m)
                        ? m.GetString() ?? "GET" : "GET";

                    // endpoint label — human-readable path for UI display
                    var endpoint = labels.TryGetProperty("endpoint", out var ep)
                        ? ep.GetString() ?? uri : uri;

                    // description label — optional
                    var description = labels.TryGetProperty("description", out var desc)
                        ? desc.GetString() : null;

                    // Verify microservice exists in DB
                    var msExists = await conn.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM microservices WHERE id = @Id AND isactive = TRUE",
                        new { Id = microserviceId });

                    if (msExists == 0)
                    {
                        result.Skipped++;
                        result.SkipReasons.Add(
                            $"Route '{routeId}' skipped — microservice_id={microserviceId} not found in microservices table");
                        continue;
                    }

                    // Check if this route already exists in DB
                    var existing = await conn.ExecuteScalarAsync<int>(
                        "SELECT COUNT(1) FROM microserviceroutes WHERE routeid = @RouteId",
                        new { RouteId = routeId });

                    // Upsert
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
                            RouteId     = routeId,
                            Method      = method.ToUpper(),
                            Path        = endpoint,
                            Description = description
                        });

                    result.Synced++;

                    if (existing == 0)
                        result.Added.Add($"{method.ToUpper()} {endpoint} (route: {routeId})");
                    else
                        result.Updated.Add($"{method.ToUpper()} {endpoint} (route: {routeId})");

                    Log.Information(
                        "RouteSyncService: {Status} route '{RouteId}' → microservice_id={MsId}",
                        existing == 0 ? "ADDED" : "UPDATED", routeId, microserviceId);
                }

                // ── 3. Mark routes deleted from APISix as inactive ───────────
                var activeRouteIds = list.EnumerateArray()
                    .Where(i => i.TryGetProperty("value", out _))
                    .Select(i => {
                        i.TryGetProperty("value", out var v);
                        return v.TryGetProperty("id", out var id) ? id.GetString() : null;
                    })
                    .Where(id => id != null)
                    .ToList();

                if (activeRouteIds.Count > 0)
                {
                    // Mark any DB route not in APISix list as inactive
                    var inClause = string.Join(",", activeRouteIds.Select((_, i) => $"@r{i}"));
                    var param    = new DynamicParameters();
                    for (int i = 0; i < activeRouteIds.Count; i++)
                        param.Add($"r{i}", activeRouteIds[i]);

                    await conn.ExecuteAsync(
                        $"UPDATE microserviceroutes SET isactive = FALSE " +
                        $"WHERE routeid NOT IN ({inClause})",
                        param);
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