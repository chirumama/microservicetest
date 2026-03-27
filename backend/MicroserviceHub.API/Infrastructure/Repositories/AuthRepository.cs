using Dapper;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.Interfaces;
using Microsoft.Data.SqlClient;
using MicroserviceHub.API.Domain.Entities;

namespace MicroserviceHub.API.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<User> GetUserByEmail(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            var query = "SELECT * FROM Users WHERE Email = @Email";
            return await connection.QueryFirstOrDefaultAsync<User>(query, new { Email = email });
        }

        public async Task CreateUser(CreateUserRequest request)
        {
            using var connection = new SqlConnection(_connectionString);

            // Hash password with BCrypt before storing
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var query = @"
    INSERT INTO Users (Email, PasswordHash, RoleId, IsActive, CreatedAt)
    VALUES (@Email, @PasswordHash, @RoleId, 1, GETUTCDATE())";

            await connection.ExecuteAsync(query, new
            {
                Email    = request.Email,
                PasswordHash = hashedPassword,
                RoleId   = request.RoleId
            });
        }
        public async Task<List<UserSummaryResponse>> GetAllUsers()
{
    using var connection = new SqlConnection(_connectionString);
    var query = @"
        SELECT u.Id, u.Email, r.Name AS Role, u.IsActive, u.CreatedAt
        FROM Users u
        JOIN Roles r ON u.RoleId = r.Id
        ORDER BY u.CreatedAt DESC";
    return (await connection.QueryAsync<UserSummaryResponse>(query)).ToList();
}

public async Task SetUserActive(int userId, bool isActive)
{
    using var connection = new SqlConnection(_connectionString);
    await connection.ExecuteAsync(
        "UPDATE Users SET IsActive = @IsActive, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
        new { Id = userId, IsActive = isActive });
}
    }
}
