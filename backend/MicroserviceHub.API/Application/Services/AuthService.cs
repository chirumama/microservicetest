using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Utilities;
using Serilog;

namespace MicroserviceHub.API.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly JwtTokenGenerator _jwt;

        public AuthService(IAuthRepository authRepository, JwtTokenGenerator jwt)
        {
            _authRepository = authRepository;
            _jwt = jwt;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            Log.Information("Login attempt for Email: {Email}", request.Email);

            var user = await _authRepository.GetUserByEmail(request.Email);

            if (user == null)
            {
                Log.Warning("Login failed - User not found for Email: {Email}", request.Email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // BCrypt verify — works whether hash is a real BCrypt hash or plain text (for legacy seed data)
            bool passwordValid = VerifyPassword(request.Password, user.PasswordHash);

            if (!passwordValid)
            {
                Log.Warning("Login failed - Invalid password for Email: {Email}", request.Email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            Log.Information("Login successful for UserId: {UserId}", user.Id);

            var token = _jwt.GenerateToken(user.Id, user.RoleId);

            // Return role name string, not the numeric ID
            var roleName = user.RoleId switch
            {
                1 => "User",
                2 => "Admin",
                3 => "SuperAdmin",
                _ => "User"
            };

            return new LoginResponse
            {
                Token = token,
                Role  = roleName
            };
        }

        public async Task CreateUserAsync(CreateUserRequest request)
        {
            await _authRepository.CreateUser(request);
        }
        private static bool VerifyPassword(string plainPassword, string storedHash)
{
    // BCrypt hashes always start with $2a$ or $2b$
    if (storedHash.StartsWith("$2a$") || storedHash.StartsWith("$2b$"))
    {
        try { return BCrypt.Net.BCrypt.Verify(plainPassword, storedHash); }
        catch { return false; }
    }

    // Plain-text fallback for seed users where PasswordHash = '123'
    return storedHash == plainPassword;
}
public async Task<List<UserSummaryResponse>> GetUsersAsync()
{
    return await _authRepository.GetAllUsers();
}
    }
}
