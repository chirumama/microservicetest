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
            // PostgreSQL uses RETURNING instead of SCOPE_IDENTITY / OUTPUT
            var query = @"
                INSERT INTO applications (user_id, title, description, status, created_at)
                VALUES (@UserId, @Title, @Description, @Status, @CreatedAt)
                RETURNING id";
            return await connection.ExecuteScalarAsync<int>(query, app);
        }

        public async Task DeleteApplication(int appId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM api_keys WHERE application_id = @Id", new { Id = appId });
            await connection.ExecuteAsync(
                "DELETE FROM applications WHERE id = @Id", new { Id = appId });
        }

        public async Task UpdateApiKeyAndSecret(int keyId, string newAppKey, string newAppSecret)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE api_keys
                SET app_key        = @AppKey,
                    app_secret_hash = @AppSecret,
                    updated_at     = NOW()
                WHERE id = @Id",
                new { Id = keyId, AppKey = newAppKey, AppSecret = newAppSecret });
        }

        public async Task CreateApiKey(int appId, string environment, string apiKey, string apiSecret)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            var consumerKey = $"{appId}_{environment.Replace("-", "_").Replace(" ", "_")}";
            await connection.ExecuteAsync(@"
                INSERT INTO api_keys
                    (application_id, environment, app_key, app_secret_hash,
                     consumer_key, is_active, is_environment_enabled, created_at)
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
            var result = await connection.QueryAsync<GetApplicationResponse>(@"
                SELECT a.id, a.title, a.description, u.email AS ownerEmail
                FROM applications a
                JOIN users u ON a.user_id = u.id
                WHERE a.user_id = @UserId",
                new { UserId = userId });
            return result.ToList();
        }

        public async Task<List<GetApplicationResponse>> GetAllApplications()
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QueryAsync<GetApplicationResponse>(@"
                SELECT a.id, a.title, a.description, u.email AS ownerEmail
                FROM applications a
                JOIN users u ON a.user_id = u.id");
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

            var environments = (await connection.QueryAsync<EnvironmentDto>(@"
                SELECT id, environment, app_key AS apiKey, app_secret_hash AS apiSecret,
                       is_environment_enabled AS isEnabled
                FROM api_keys
                WHERE application_id = @AppId",
                new { AppId = appId })).ToList();

            // PostgreSQL uses COALESCE instead of ISNULL, and BOOL instead of INT for IsEnabled
            var microservices = (await connection.QueryAsync<MicroserviceDto>(@"
                SELECT m.id, m.name,
                       COALESCE(am.is_enabled, FALSE) AS isEnabled
                FROM microservices m
                LEFT JOIN application_microservices am
                    ON m.id = am.microservice_id AND am.application_id = @AppId",
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
            await connection.ExecuteAsync(@"
                UPDATE api_keys
                SET is_environment_enabled = @IsEnabled, updated_at = NOW()
                WHERE application_id = @AppId AND environment = @Environment",
                new { AppId = appId, Environment = environment, IsEnabled = isEnabled });
        }

        public async Task<ApiKeyFullRecord?> GetApiKeyByAppKey(string appKey)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ApiKeyFullRecord>(@"
                SELECT id, application_id AS applicationid, environment, app_key AS appkey,
                       app_secret_hash AS appsecretHash, is_active AS isactive,
                       is_environment_enabled AS isenvironmentenabled
                FROM api_keys
                WHERE app_key = @AppKey",
                new { AppKey = appKey });
        }

        public async Task UpsertMicroservice(int appId, int microserviceId, bool isEnabled)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // PostgreSQL INSERT … ON CONFLICT replaces MSSQL IF EXISTS / MERGE
            await connection.ExecuteAsync(@"
                INSERT INTO application_microservices (application_id, microservice_id, is_enabled, created_at)
                VALUES (@AppId, @MicroserviceId, @IsEnabled, NOW())
                ON CONFLICT (application_id, microservice_id)
                DO UPDATE SET is_enabled = @IsEnabled, updated_at = NOW()",
                new { AppId = appId, MicroserviceId = microserviceId, IsEnabled = isEnabled });
        }

        public async Task UpdateApiSecret(int keyId, string newSecret)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE api_keys SET app_secret_hash = @Secret, updated_at = NOW() WHERE id = @Id",
                new { Id = keyId, Secret = newSecret });
        }

        public async Task RevokeApiKey(int keyId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                UPDATE api_keys
                SET is_active = FALSE, is_environment_enabled = FALSE, updated_at = NOW()
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
            var result = await connection.QueryFirstOrDefaultAsync<ApiKeyInfoResponse>(@"
                SELECT id, application_id AS applicationId, environment, app_key AS appKey,
                       COALESCE(consumer_key,
                           CAST(application_id AS TEXT) || '_' ||
                           REPLACE(REPLACE(environment, '-', '_'), ' ', '_')
                       ) AS consumerKey
                FROM api_keys
                WHERE id = @Id",
                new { Id = keyId });

            if (result == null)
                throw new KeyNotFoundException($"ApiKey with Id {keyId} not found");

            return result;
        }

        public async Task SaveAccessToken(int keyId, string accessToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE api_keys SET access_token = @Token WHERE id = @Id",
                new { Id = keyId, Token = accessToken });
        }

        public async Task<string?> GetAccessToken(int keyId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT access_token FROM api_keys WHERE id = @Id",
                new { Id = keyId });
        }

        public async Task UpdateConsumerKey(int keyId, string newConsumerKey)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE api_keys SET consumer_key = @ConsumerKey WHERE id = @Id",
                new { Id = keyId, ConsumerKey = newConsumerKey });
        }

        public async Task<string> GetConsumerKeyByAppKey(string appKey)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QueryFirstOrDefaultAsync<string>(@"
                SELECT COALESCE(consumer_key,
                    CAST(application_id AS TEXT) || '_' ||
                    REPLACE(REPLACE(environment, '-', '_'), ' ', '_'))
                FROM api_keys WHERE app_key = @AppKey",
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
        }}}