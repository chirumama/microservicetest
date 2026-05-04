using Dapper;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Domain.Entities;
using MicroserviceHub.API.Infrastructure.ExternalServices;
using Npgsql;

namespace MicroserviceHub.API.Infrastructure.Repositories
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly string _connectionString;
        private NpgsqlConnection? _connection;
        private NpgsqlTransaction? _transaction;

        public ApplicationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<int> CreateApplication(Domain.Entities.Application app)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: DB columns are lowercase (userid, createdat) — no underscores
            var query = @"
                INSERT INTO applications (userid, title, description, status, createdat)
                VALUES (@UserId, @Title, @Description, @Status, @CreatedAt)
                RETURNING id";
            return await connection.ExecuteScalarAsync<int>(query, app);
        }

        public async Task DeleteApplication(int appId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: apikeys.applicationid (no underscore)
            await connection.ExecuteAsync(
                "DELETE FROM apikeys WHERE applicationid = @Id", new { Id = appId });
            await connection.ExecuteAsync(
                "DELETE FROM applications WHERE id = @Id", new { Id = appId });
        }

        public async Task UpdateApiKeyAndSecret(int keyId, string newAppKey, string newAppSecret)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: apikeys columns are appkey, appsecretHash, updatedat
            await connection.ExecuteAsync(@"
                UPDATE apikeys
                SET appkey          = @AppKey,
                    appsecretHash   = @AppSecret,
                    updatedat       = NOW()
                WHERE id = @Id",
                new { Id = keyId, AppKey = newAppKey, AppSecret = newAppSecret });
        }

        public async Task CreateApiKey(int appId, string environment, string apiKey, string apiSecret)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            var consumerKey = $"{appId}_{environment.Replace("-", "_").Replace(" ", "_")}";
            // FIX: columns applicationid, appkey, appsecretHash, consumerkey, isactive, isenvironmentenabled, createdat
            await connection.ExecuteAsync(@"
                INSERT INTO apikeys
                    (applicationid, environment, appkey, appsecretHash,
                     consumerkey, isactive, isenvironmentenabled, createdat)
                VALUES
                    (@ApplicationId, @Environment, @ApiKey, @ApiSecretHash,
                     @ConsumerKey, TRUE, TRUE, NOW())",
                new
                {
                    ApplicationId = appId,
                    Environment   = environment,
                    ApiKey        = apiKey,
                    ApiSecretHash = apiSecret,
                    ConsumerKey   = consumerKey
                });
        }

        public async Task<List<GetApplicationResponse>> GetApplicationsByUser(int userId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: applications.userid, users.id (no underscore joins)
            var result = await connection.QueryAsync<GetApplicationResponse>(@"
                SELECT a.id, a.title, a.description, u.email AS ownerEmail
                FROM applications a
                JOIN users u ON a.userid = u.id
                WHERE a.userid = @UserId",
                new { UserId = userId });
            return result.ToList();
        }

        public async Task<List<GetApplicationResponse>> GetAllApplications()
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: applications.userid
            var result = await connection.QueryAsync<GetApplicationResponse>(@"
                SELECT a.id, a.title, a.description, u.email AS ownerEmail
                FROM applications a
                JOIN users u ON a.userid = u.id");
            return result.ToList();
        }

        public async Task<GetApplicationDetailsResponse> GetApplicationDetails(int appId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            var app = await connection.QueryFirstOrDefaultAsync(
                "SELECT id, title FROM applications WHERE id = @Id",
                new { Id = appId });

            if (app == null)
                throw new KeyNotFoundException($"Application {appId} not found");

            // FIX: apikeys columns — applicationid, appkey, appsecretHash, isenvironmentenabled
            var environments = (await connection.QueryAsync<EnvironmentDto>(@"
                SELECT id, environment,
                       appkey        AS apiKey,
                       appsecretHash AS apiSecret,
                       isenvironmentenabled AS isEnabled
                FROM apikeys
                WHERE applicationid = @AppId",
                new { AppId = appId })).ToList();

            // FIX: applicationmicroservices columns — microserviceid, applicationid, isenabled
            var microservices = (await connection.QueryAsync<MicroserviceDto>(@"
                SELECT m.id, m.name,
                       COALESCE(am.isenabled, FALSE) AS isEnabled
                FROM microservices m
                LEFT JOIN applicationmicroservices am
                    ON m.id = am.microserviceid AND am.applicationid = @AppId",
                new { AppId = appId })).ToList();

            return new GetApplicationDetailsResponse
            {
                ApplicationId = app.id,
                Title         = app.title,
                Environments  = environments,
                Microservices = microservices
            };
        }

        public async Task UpdateEnvironment(int appId, string environment, bool isEnabled)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: apikeys.isenvironmentenabled, applicationid, updatedat
            await connection.ExecuteAsync(@"
                UPDATE apikeys
                SET isenvironmentenabled = @IsEnabled, updatedat = NOW()
                WHERE applicationid = @AppId AND environment = @Environment",
                new { AppId = appId, Environment = environment, IsEnabled = isEnabled });
        }

        public async Task<ApiKeyFullRecord?> GetApiKeyByAppKey(string appKey)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: all column aliases map to correct no-underscore DB names
            return await connection.QueryFirstOrDefaultAsync<ApiKeyFullRecord>(@"
                SELECT id,
                       applicationid       AS applicationid,
                       environment,
                       appkey              AS appkey,
                       appsecretHash       AS appsecretHash,
                       isactive            AS isactive,
                       isenvironmentenabled AS isenvironmentenabled
                FROM apikeys
                WHERE appkey = @AppKey",
                new { AppKey = appKey });
        }

        public async Task UpsertMicroservice(int appId, int microserviceId, bool isEnabled)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: applicationmicroservices columns — applicationid, microserviceid, isenabled, createdat
            // UNIQUE constraint is (applicationid, microserviceid)
            await connection.ExecuteAsync(@"
                INSERT INTO applicationmicroservices (applicationid, microserviceid, isenabled, createdat)
                VALUES (@AppId, @MicroserviceId, @IsEnabled, NOW())
                ON CONFLICT (applicationid, microserviceid)
                DO UPDATE SET isenabled = @IsEnabled, updatedat = NOW()",
                new { AppId = appId, MicroserviceId = microserviceId, IsEnabled = isEnabled });
        }

        public async Task UpdateApiSecret(int keyId, string newSecret)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: apikeys.appsecretHash, updatedat
            await connection.ExecuteAsync(@"
                UPDATE apikeys SET appsecretHash = @Secret, updatedat = NOW() WHERE id = @Id",
                new { Id = keyId, Secret = newSecret });
        }

        public async Task RevokeApiKey(int keyId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: apikeys.isactive, isenvironmentenabled, updatedat
            await connection.ExecuteAsync(@"
                UPDATE apikeys
                SET isactive = FALSE, isenvironmentenabled = FALSE, updatedat = NOW()
                WHERE id = @Id",
                new { Id = keyId });
        }

        public async Task<IEnumerable<Microservice>> GetMicroservicesAsync()
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Microservice>(
                "SELECT id, name, description FROM microservices");
        }

        public async Task<Domain.Entities.Application> GetApplicationById(int appId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Domain.Entities.Application>(
                "SELECT * FROM applications WHERE id = @Id", new { Id = appId });
        }

        // ── Transaction support ───────────────────────────────────────────────
        public async Task BeginTransaction()
        {
            _connection = new NpgsqlConnection(_connectionString);
            await _connection.OpenAsync();
            _transaction = await _connection.BeginTransactionAsync();
        }

        public async Task CommitTransaction()
        {
            if (_transaction != null) await _transaction.CommitAsync();
            if (_connection  != null) await _connection.CloseAsync();
        }

        public async Task RollbackTransaction()
        {
            if (_transaction != null) await _transaction.RollbackAsync();
            if (_connection  != null) await _connection.CloseAsync();
        }

        public async Task<ApiKeyInfoResponse> GetApiKeyById(int keyId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: applicationid, appkey, consumerkey (no underscores)
            var result = await connection.QueryFirstOrDefaultAsync<ApiKeyInfoResponse>(@"
                SELECT id,
                       applicationid AS applicationId,
                       environment,
                       appkey        AS appKey,
                       COALESCE(consumerkey,
                           CAST(applicationid AS TEXT) || '_' ||
                           REPLACE(REPLACE(environment, '-', '_'), ' ', '_')
                       ) AS consumerKey
                FROM apikeys
                WHERE id = @Id",
                new { Id = keyId });

            if (result == null)
                throw new KeyNotFoundException($"ApiKey with Id {keyId} not found");

            return result;
        }

        public async Task SaveAccessToken(int keyId, string accessToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: apikeys.accesstoken (no underscore)
            await connection.ExecuteAsync(
                "UPDATE apikeys SET accesstoken = @Token WHERE id = @Id",
                new { Id = keyId, Token = accessToken });
        }

        public async Task<string?> GetAccessToken(int keyId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: apikeys.accesstoken
            return await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT accesstoken FROM apikeys WHERE id = @Id",
                new { Id = keyId });
        }

        public async Task UpdateConsumerKey(int keyId, string newConsumerKey)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: apikeys.consumerkey
            await connection.ExecuteAsync(
                "UPDATE apikeys SET consumerkey = @ConsumerKey WHERE id = @Id",
                new { Id = keyId, ConsumerKey = newConsumerKey });
        }

        public async Task<string> GetConsumerKeyByAppKey(string appKey)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: consumerkey, applicationid, appkey
            var result = await connection.QueryFirstOrDefaultAsync<string>(@"
                SELECT COALESCE(consumerkey,
                    CAST(applicationid AS TEXT) || '_' ||
                    REPLACE(REPLACE(environment, '-', '_'), ' ', '_'))
                FROM apikeys WHERE appkey = @AppKey",
                new { AppKey = appKey });
            return result ?? throw new KeyNotFoundException("AppKey not found");
        }


        // ── Route methods ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns all routes for a microservice, annotated with their enabled
        /// state for the given application. Defaults to TRUE if no row exists yet.
        /// </summary>
        public async Task<List<MicroserviceRouteDto>> GetRoutesForAppAsync(int appId, int microserviceId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: microserviceroutes and applicationroutes use no-underscore column names
            var rows = await connection.QueryAsync<MicroserviceRouteDto>(@"
                SELECT
                    mr.routeid      AS routeid,
                    mr.method       AS method,
                    mr.path         AS path,
                    mr.description  AS description,
                    COALESCE(ar.isenabled, TRUE) AS isenabled
                FROM microserviceroutes mr
                LEFT JOIN applicationroutes ar
                    ON ar.routeid       = mr.routeid
                    AND ar.applicationid = @AppId
                WHERE mr.microserviceid = @MsId
                  AND mr.isactive = TRUE
                ORDER BY mr.id",
                new { AppId = appId, MsId = microserviceId });
            return rows.ToList();
        }

        /// <summary>
        /// Returns all routes for a microservice (no app-specific state).
        /// Used when seeding default enabled=true rows.
        /// </summary>
        public async Task<List<MicroserviceRoute>> GetMicroserviceRoutesAsync(int microserviceId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: microserviceroutes column names — microserviceid, routeid, isactive, createdat
            var rows = await connection.QueryAsync<MicroserviceRoute>(@"
                SELECT id, microserviceid, routeid, method, path, description, isactive, createdat
                FROM microserviceroutes
                WHERE microserviceid = @MsId AND isactive = TRUE
                ORDER BY id",
                new { MsId = microserviceId });
            return rows.ToList();
        }

        /// <summary>
        /// Upserts the enabled/disabled state for a specific route within an application.
        /// </summary>
        public async Task UpsertApplicationRouteAsync(int appId, int microserviceId, string routeId, bool isEnabled)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: applicationroutes columns — applicationid, microserviceid, routeid, isenabled, createdat, updatedat
            // UNIQUE constraint is (applicationid, routeid)
            await connection.ExecuteAsync(@"
                INSERT INTO applicationroutes (applicationid, microserviceid, routeid, isenabled, createdat)
                VALUES (@AppId, @MsId, @RouteId, @IsEnabled, NOW())
                ON CONFLICT (applicationid, routeid)
                DO UPDATE SET isenabled = @IsEnabled, updatedat = NOW()",
                new { AppId = appId, MsId = microserviceId, RouteId = routeId, IsEnabled = isEnabled });
        }

        /// <summary>
        /// Returns all enabled routeIds for a given application + microservice.
        /// Used by APISix sync.
        /// </summary>
        public async Task<List<string>> GetEnabledRouteIdsAsync(int appId, int microserviceId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // FIX: microserviceroutes.microserviceid, routeid, isactive; applicationroutes.applicationid, isenabled
            var rows = await connection.QueryAsync<string>(@"
                SELECT mr.routeid
                FROM microserviceroutes mr
                LEFT JOIN applicationroutes ar
                    ON ar.routeid = mr.routeid AND ar.applicationid = @AppId
                WHERE mr.microserviceid = @MsId
                  AND mr.isactive = TRUE
                  AND COALESCE(ar.isenabled, TRUE) = TRUE",
                new { AppId = appId, MsId = microserviceId });
            return rows.ToList();
        }
                public async Task<int> UpsertMicroserviceByNameAsync(string name, string description)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
 
            // Try to find an existing row first
            var existing = await connection.QueryFirstOrDefaultAsync<int?>(@"
                SELECT id FROM microservices WHERE name = @Name",
                new { Name = name });
 
            if (existing.HasValue)
                return existing.Value;
 
            // Not found — insert and return new id
            var newId = await connection.ExecuteScalarAsync<int>(@"
                INSERT INTO microservices (name, description, isactive, createdat)
                VALUES (@Name, @Description, TRUE, NOW())
                ON CONFLICT DO NOTHING
                RETURNING id",
                new { Name = name, Description = description });
 
            // Edge case: two concurrent requests could both reach here;
            // ON CONFLICT DO NOTHING returns 0 rows, so re-query.
            if (newId == 0)
            {
                newId = await connection.QueryFirstOrDefaultAsync<int>(@"
                    SELECT id FROM microservices WHERE name = @Name",
                    new { Name = name });
            }
 
            return newId;
        }
public async Task UpsertMicroserviceRouteAsync(
            int    microserviceId,
            string routeId,
            string method,
            string path,
            string description)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                INSERT INTO microserviceroutes
                    (microserviceid, routeid, method, path, description, isactive, createdat)
                VALUES
                    (@MicroserviceId, @RouteId, @Method, @Path, @Description, TRUE, NOW())
                ON CONFLICT (microserviceid, routeid) DO NOTHING",
                new
                {
                    MicroserviceId = microserviceId,
                    RouteId        = routeId,
                    Method         = method,
                    Path           = path,
                    Description    = description,
                });
        } 
    }
}