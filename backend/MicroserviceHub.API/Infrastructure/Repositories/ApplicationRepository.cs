using Dapper;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Domain.Entities;
using Microsoft.Data.SqlClient;
using MicroserviceHub.API.Infrastructure.ExternalServices;

namespace MicroserviceHub.API.Infrastructure.Repositories
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly string _connectionString;
        private SqlConnection? _connection;
        private SqlTransaction? _transaction;

        public ApplicationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<int> CreateApplication(Domain.Entities.Application app)
        {
            using var connection = new SqlConnection(_connectionString);

            var query = @"
        INSERT INTO Applications (UserId, Title, Description, Status, CreatedAt)
        VALUES (@UserId, @Title, @Description, @Status, @CreatedAt);
        SELECT CAST(SCOPE_IDENTITY() as int);";

            return await connection.ExecuteScalarAsync<int>(query, app);
        }
public async Task DeleteApplication(int appId)
{
    using var connection = new SqlConnection(_connectionString);
    // Delete keys first (FK constraint), then the application
    await connection.ExecuteAsync(
        "DELETE FROM ApiKeys WHERE ApplicationId = @Id", new { Id = appId });
    await connection.ExecuteAsync(
        "DELETE FROM Applications WHERE Id = @Id", new { Id = appId });
}
public async Task UpdateApiKeyAndSecret(int keyId, string newAppKey, string newAppSecret)
{
    using var connection = new SqlConnection(_connectionString);

    var query = @"
        UPDATE ApiKeys
        SET AppKey        = @AppKey,
            AppSecretHash = @AppSecret,
            UpdatedAt     = GETUTCDATE()
        WHERE Id = @Id";

    await connection.ExecuteAsync(query, new
    {
        Id        = keyId,
        AppKey    = newAppKey,
        AppSecret = newAppSecret
    });
}
        public async Task CreateApiKey(int appId, string environment, string apiKey, string apiSecret)
        {
            using var connection = new SqlConnection(_connectionString);

            // Column name in DB is AppKey / AppSecretHash (matches the schema you provided)
            var query = @"
        INSERT INTO ApiKeys (ApplicationId, Environment, AppKey, AppSecretHash, IsActive, IsEnvironmentEnabled, CreatedAt)
        VALUES (@ApplicationId, @Environment, @ApiKey, @ApiSecretHash, 1, 1, GETUTCDATE())";

            await connection.ExecuteAsync(query, new
            {
                ApplicationId = appId,
                Environment   = environment,
                ApiKey        = apiKey,
                ApiSecretHash = apiSecret
            });
        }

        public async Task<List<GetApplicationResponse>> GetApplicationsByUser(int userId)
        {
            using var connection = new SqlConnection(_connectionString);

            var query = @"
    SELECT a.Id, a.Title, a.Description, u.Email AS OwnerEmail
    FROM Applications a
    JOIN Users u ON a.UserId = u.Id
    WHERE a.UserId = @UserId";

            return (await connection.QueryAsync<GetApplicationResponse>(query, new { UserId = userId })).ToList();
        }

        public async Task<List<GetApplicationResponse>> GetAllApplications()
        {
            using var connection = new SqlConnection(_connectionString);

            var query = @"
    SELECT a.Id, a.Title, a.Description, u.Email AS OwnerEmail
    FROM Applications a
    JOIN Users u ON a.UserId = u.Id";

            return (await connection.QueryAsync<GetApplicationResponse>(query)).ToList();
        }

        public async Task<GetApplicationDetailsResponse> GetApplicationDetails(int appId)
        {
            using var connection = new SqlConnection(_connectionString);

            // 1. Get Application
            var appQuery = "SELECT Id, Title FROM Applications WHERE Id = @Id";
            var app = await connection.QueryFirstOrDefaultAsync(appQuery, new { Id = appId });

            if (app == null)
                throw new KeyNotFoundException($"Application {appId} not found");

            // 2. Get Api Keys — AppKey/AppSecretHash are the actual DB column names
            var envQuery = @"
    SELECT Id, Environment, AppKey AS ApiKey, AppSecretHash AS ApiSecret, IsEnvironmentEnabled AS IsEnabled
    FROM ApiKeys
    WHERE ApplicationId = @AppId";

            var environments = (await connection.QueryAsync<EnvironmentDto>(envQuery, new { AppId = appId })).ToList();

            // 3. Get Microservices
            var microQuery = @"
    SELECT m.Id, m.Name,
           ISNULL(am.IsEnabled, 0) AS IsEnabled
    FROM Microservices m
    LEFT JOIN ApplicationMicroservices am
        ON m.Id = am.MicroserviceId AND am.ApplicationId = @AppId";

            var microservices = (await connection.QueryAsync<MicroserviceDto>(microQuery, new { AppId = appId })).ToList();

            return new GetApplicationDetailsResponse
            {
                ApplicationId = app.Id,
                Title         = app.Title,
                Environments  = environments,
                Microservices = microservices
            };
        }

        public async Task UpdateEnvironment(int appId, string environment, bool isEnabled)
        {
            using var connection = new SqlConnection(_connectionString);

            var query = @"
    UPDATE ApiKeys
    SET IsEnvironmentEnabled = @IsEnabled,
        UpdatedAt = GETUTCDATE()
    WHERE ApplicationId = @AppId AND Environment = @Environment";

            await connection.ExecuteAsync(query, new
            {
                AppId       = appId,
                Environment = environment,
                IsEnabled   = isEnabled
            });
        }


        public async Task<ApiKeyFullRecord?> GetApiKeyByAppKey(string appKey)
{
    using var connection = new SqlConnection(_connectionString);

    var query = @"
        SELECT Id, ApplicationId, Environment, AppKey,
               AppSecretHash, IsActive, IsEnvironmentEnabled
        FROM   ApiKeys
        WHERE  AppKey = @AppKey";

    return await connection.QueryFirstOrDefaultAsync<ApiKeyFullRecord>(
        query, new { AppKey = appKey });
}

        public async Task UpsertMicroservice(int appId, int microserviceId, bool isEnabled)
        {
            using var connection = new SqlConnection(_connectionString);

            var query = @"
    IF EXISTS (
        SELECT 1 FROM ApplicationMicroservices
        WHERE ApplicationId = @AppId AND MicroserviceId = @MicroserviceId
    )
    BEGIN
        UPDATE ApplicationMicroservices
        SET IsEnabled = @IsEnabled, UpdatedAt = GETUTCDATE()
        WHERE ApplicationId = @AppId AND MicroserviceId = @MicroserviceId
    END
    ELSE
    BEGIN
        INSERT INTO ApplicationMicroservices (ApplicationId, MicroserviceId, IsEnabled, CreatedAt)
        VALUES (@AppId, @MicroserviceId, @IsEnabled, GETUTCDATE())
    END";

            await connection.ExecuteAsync(query, new
            {
                AppId          = appId,
                MicroserviceId = microserviceId,
                IsEnabled      = isEnabled
            });
        }

        public async Task UpdateApiSecret(int keyId, string newSecret)
        {
            using var connection = new SqlConnection(_connectionString);

            var query = @"
    UPDATE ApiKeys
    SET AppSecretHash = @Secret,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @Id";

            await connection.ExecuteAsync(query, new { Id = keyId, Secret = newSecret });
        }

        public async Task RevokeApiKey(int keyId)
        {
            using var connection = new SqlConnection(_connectionString);

            var query = @"
    UPDATE ApiKeys
    SET IsActive = 0,
        IsEnvironmentEnabled = 0,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @Id";

            await connection.ExecuteAsync(query, new { Id = keyId });
        }

        public async Task<IEnumerable<Microservice>> GetMicroservicesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<Microservice>("SELECT Id, Name, Description FROM Microservices");
        }

        public async Task<Domain.Entities.Application> GetApplicationById(int appId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Domain.Entities.Application>(
                "SELECT * FROM Applications WHERE Id = @Id", new { Id = appId });
        }

        public async Task BeginTransaction()
        {
            _connection = new SqlConnection(_connectionString);
            await _connection.OpenAsync();
            _transaction = _connection.BeginTransaction();
        }

        public async Task CommitTransaction()
        {
            _transaction?.Commit();
            if (_connection != null) await _connection.CloseAsync();
        }

        public async Task RollbackTransaction()
        {
            _transaction?.Rollback();
            if (_connection != null) await _connection.CloseAsync();
        }
        public async Task<ApiKeyInfoResponse> GetApiKeyById(int keyId)
{
    using var connection = new SqlConnection(_connectionString);

    var query = @"
        SELECT Id, ApplicationId, Environment, AppKey
        FROM ApiKeys
        WHERE Id = @Id";

    var result = await connection.QueryFirstOrDefaultAsync<ApiKeyInfoResponse>(
        query, new { Id = keyId });

    if (result == null)
        throw new KeyNotFoundException($"ApiKey with Id {keyId} not found");

    return result;
}
    }
}
