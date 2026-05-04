using Dapper;
using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Domain.Entities;
using Npgsql;

namespace MicroserviceHub.API.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // ── User methods ──────────────────────────────────────────────────────

        public async Task<User?> GetUserByEmail(string email)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE email = @Email",
                new { Email = email });
        }

        public async Task CreateUser(CreateUserRequest request)
        {
            // Strong password validation (server-side guard)
            var passwordRegex = new System.Text.RegularExpressions.Regex(
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$");

            if (!passwordRegex.IsMatch(request.Password))
                throw new InvalidOperationException(
                    "Password must be at least 8 characters and include uppercase, lowercase, a number, and a special character.");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"INSERT INTO users (email, passwordhash, roleid, isactive, createdat)
  VALUES (@Email, @PasswordHash, @RoleId, TRUE, NOW())",
                new { Email = request.Email, PasswordHash = hashedPassword, RoleId = request.RoleId });
        }

        public async Task<List<UserSummaryResponse>> GetAllUsers()
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QueryAsync<UserSummaryResponse>(
                @"SELECT u.id, u.email, r.name AS role, u.isactive, u.createdat
  FROM users u
  JOIN roles r ON u.roleid = r.id
  ORDER BY u.createdat DESC");
            return result.ToList();
        }

        public async Task SetUserActive(int userId, bool isActive)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE users SET isactive = @IsActive, updatedat = NOW() WHERE id = @Id",
                new { Id = userId, IsActive = isActive });
        }

        // ── OTP methods ───────────────────────────────────────────────────────

        public async Task SaveOtpAsync(UserOtp otp)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"INSERT INTO userotps (userid, otpcode, expirytime, isused, createdat)
  VALUES (@UserId, @OtpCode, @ExpiryTime, @IsUsed, NOW())",
                new { otp.UserId, otp.OtpCode, otp.ExpiryTime, otp.IsUsed });
        }

        public async Task<UserOtp?> GetLatestOtpAsync(int userId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<UserOtp>(
                @"SELECT id, userid, otpcode, expirytime, isused, createdat
  FROM userotps
  WHERE userid = @UserId AND isused = FALSE
  ORDER BY createdat DESC
  LIMIT 1",
                new { UserId = userId });
        }

        public async Task MarkOtpUsedAsync(UserOtp otp)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
               "UPDATE userotps SET isused = TRUE WHERE id = @Id",
                new { otp.Id });
        }
    }
}