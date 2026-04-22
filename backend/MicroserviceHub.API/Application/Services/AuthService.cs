using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Utilities;
using Serilog;

namespace MicroserviceHub.API.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository    _authRepository;
        private readonly OAuthTokenService  _tokenService;
        private readonly IConfiguration     _config;

        public AuthService(
            IAuthRepository   authRepository,
            OAuthTokenService tokenService,
            IConfiguration    config)
        {
            _authRepository = authRepository;
            _tokenService   = tokenService;
            _config         = config;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            Log.Information("Login attempt for Email: {Email}", request.Email);

            var user = await _authRepository.GetUserByEmail(request.Email);

            if (user == null)
            {
                Log.Warning("Login failed — user not found: {Email}", request.Email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                Log.Warning("Login failed — wrong password: {Email}", request.Email);
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var roleName = user.RoleId switch
            {
                1 => "User",
                2 => "Admin",
                3 => "SuperAdmin",
                _ => "User"
            };

            var token = _tokenService.GenerateDashboardToken(
                userId: user.Id,
                roleId: user.RoleId,
                role:   roleName,
                email:  user.Email);

            var expiryMinutes = _config.GetValue<int>("OAuth:DashboardTokenExpiryMinutes", 60);

            Log.Information("Login successful for UserId: {UserId}", user.Id);

            return new LoginResponse
            {
                AccessToken = token,
                TokenType   = "Bearer",
                ExpiresIn   = expiryMinutes * 60,
                Role        = roleName,
                Email       = user.Email
            };
        }

        public async Task CreateUserAsync(CreateUserRequest request)
            => await _authRepository.CreateUser(request);

        public async Task<List<UserSummaryResponse>> GetUsersAsync()
            => await _authRepository.GetAllUsers();

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