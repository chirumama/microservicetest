using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using Serilog;

namespace MicroserviceHub.API.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;

        // JwtTokenGenerator removed — no JWT anywhere in this class
        public AuthService(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            Log.Information("Login attempt for Email: {Email}", request.Email);

            var user = await _authRepository.GetUserByEmail(request.Email);

            if (user == null)
            {
                Log.Warning("Login failed - User not found: {Email}", request.Email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            bool passwordValid = VerifyPassword(request.Password, user.PasswordHash);

            if (!passwordValid)
            {
                Log.Warning("Login failed - Wrong password: {Email}", request.Email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            Log.Information("Login successful for UserId: {UserId}", user.Id);

            var roleName = user.RoleId switch
            {
                1 => "User",
                2 => "Admin",
                3 => "SuperAdmin",
                _ => "User"
            };

            // Return UserId and RoleId directly.
            // Client sends these as X-User-Id and X-User-Role headers on every request.
            return new LoginResponse
            {
                UserId = user.Id,
                RoleId = user.RoleId,
                Role   = roleName,
                Email  = user.Email
            };
        }

        public async Task CreateUserAsync(CreateUserRequest request)
        {
            await _authRepository.CreateUser(request);
        }

        public async Task<List<UserSummaryResponse>> GetUsersAsync()
        {
            return await _authRepository.GetAllUsers();
        }

        private static bool VerifyPassword(string plainPassword, string storedHash)
        {
            if (storedHash.StartsWith("$2a$") || storedHash.StartsWith("$2b$"))
            {
                try { return BCrypt.Net.BCrypt.Verify(plainPassword, storedHash); }
                catch { return false; }
            }
            return storedHash == plainPassword;
        }
    }
}
