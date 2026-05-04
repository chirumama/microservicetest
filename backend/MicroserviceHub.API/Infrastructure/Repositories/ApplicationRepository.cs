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
            var query = @"INSERT INTO applications (userid, title, description, status, createdat)
  VALUES (@UserId, @Title, @Description, @Status, @CreatedAt)
  RETURNING id";
            return await connection.ExecuteScalarAsync<int>(query, app);
        }

        public async Task DeleteApplication(int appId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM apikeys WHERE applicationid = @Id", new { Id = appId });
            await connection.ExecuteAsync(
                "DELETE FROM applications WHERE id = @Id", new { Id = appId });
        }

        public async Task UpdateApiKeyAndSecret(int keyId, string newAppKey, string newAppSecret)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"UPDATE apikeys
  SET appkey = @AppKey, appsecrethash = @AppSecret, updatedat = NOW()
  WHERE id = @Id",
                new { Id = keyId, AppKey = newAppKey, AppSecret = newAppSecret });
        }

        public async Task CreateApiKey(int appId, string environment, string apiKey, string apiSecret)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            var consumerKey = $"{appId}_{environment.Replace("-", "_").Replace(" ", "_")}";
            await connection.ExecuteAsync(@"INSERT INTO apikeys (applicationid, environment, appkey, appsecrethash,
     consumerkey, isactive, isenvironmentenabled, createdat)
  VALUES (@ApplicationId, @Environment, @ApiKey, @ApiSecretHash,
     @ConsumerKey, TRUE, TRUE, NOW())"
,
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
            var result = await connection.QueryAsync<GetApplicationResponse>(@"SELECT a.id, a.title, a.description, u.email AS ownerEmail
  FROM applications a
  JOIN users u ON a.userid = u.id
  WHERE a.userid = @UserId",
                new { UserId = userId });
            return result.ToList();
        }

        public async Task<List<GetApplicationResponse>> GetAllApplications()
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QueryAsync<GetApplicationResponse>(@"SELECT a.id, a.title, a.description, u.email AS ownerEmail
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

            var environments = (await connection.QueryAsync<EnvironmentDto>(@"SELECT id, environment, appkey AS apiKey, appsecrethash AS apiSecret,
         isenvironmentenabled AS isEnabled
  FROM apikeys
  WHERE applicationid = @AppId",
                new { AppId = appId })).ToList();

            // PostgreSQL uses COALESCE instead of ISNULL, and BOOL instead of INT for IsEnabled
            var microservices = (await connection.QueryAsync<MicroserviceDto>(@"SELECT m.id, m.name,
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
            await connection.ExecuteAsync(@"UPDATE apikeys
  SET isenvironmentenabled = @IsEnabled, updatedat = NOW()
  WHERE applicationid = @AppId AND environment = @Environment",
                new { AppId = appId, Environment = environment, IsEnabled = isEnabled });
        }

        public async Task<ApiKeyFullRecord?> GetApiKeyByAppKey(string appKey)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ApiKeyFullRecord>(@"SELECT id, applicationid, environment, appkey,
         appsecrethash, isactive, isenvironmentenabled
  FROM apikeys WHERE appkey = @AppKey",
                new { AppKey = appKey });
        }

        public async Task UpsertMicroservice(int appId, int microserviceId, bool isEnabled)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            // PostgreSQL INSERT … ON CONFLICT replaces MSSQL IF EXISTS / MERGE
            await connection.ExecuteAsync(@"INSERT INTO applicationmicroservices (applicationid, microserviceid, isenabled, createdat)
  VALUES (@AppId, @MicroserviceId, @IsEnabled, NOW())
  ON CONFLICT (applicationid, microserviceid)
  DO UPDATE SET isenabled = @IsEnabled, updatedat = NOW()",
                new { AppId = appId, MicroserviceId = microserviceId, IsEnabled = isEnabled });
        }

        public async Task UpdateApiSecret(int keyId, string newSecret)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("UPDATE apikeys SET appsecrethash = @Secret, updatedat = NOW() WHERE id = @Id",
                new { Id = keyId, Secret = newSecret });
        }

        public async Task RevokeApiKey(int keyId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"UPDATE apikeys
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

        public async Task<Domain.Entities.Application?> GetApplicationById(int appId)
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
            var result = await connection.QueryFirstOrDefaultAsync<ApiKeyInfoResponse>(@"SELECT id, applicationid, environment, appkey,
         COALESCE(consumerkey,
             CAST(applicationid AS TEXT) || '_' ||
             REPLACE(REPLACE(environment, '-', '_'), ' ', '_')
         ) AS consumerKey
  FROM apikeys WHERE id = @Id",
                new { Id = keyId });

            if (result == null)
                throw new KeyNotFoundException($"ApiKey with Id {keyId} not found");

            return result;
        }

        public async Task SaveAccessToken(int keyId, string accessToken)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE apikeys SET accesstoken = @Token WHERE id = @Id",
                new { Id = keyId, Token = accessToken });
        }

        public async Task<string?> GetAccessToken(int keyId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT accesstoken FROM apikeys WHERE id = @Id",
                new { Id = keyId });
        }

        public async Task UpdateConsumerKey(int keyId, string newConsumerKey)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE apikeys SET consumerkey = @ConsumerKey WHERE id = @Id",
                new { Id = keyId, ConsumerKey = newConsumerKey });
        }

        public async Task<string> GetConsumerKeyByAppKey(string appKey)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QueryFirstOrDefaultAsync<string>(@"SELECT COALESCE(consumerkey,
      CAST(applicationid AS TEXT) || '_' ||
      REPLACE(REPLACE(environment, '-', '_'), ' ', '_'))
  FROM apikeys WHERE appkey = @AppKey",
                new { AppKey = appKey });
            return result ?? throw new KeyNotFoundException("AppKey not found");
        }
    }
}