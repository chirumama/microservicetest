using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Domain.Entities;
using MicroserviceHub.API.Utilities;
using Serilog;

namespace MicroserviceHub.API.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository   _authRepository;
        private readonly OAuthTokenService _tokenService;
        private readonly IConfiguration    _config;

        public AuthService(
            IAuthRepository   authRepository,
            OAuthTokenService tokenService,
            IConfiguration    config)
        {
            _authRepository = authRepository;
            _tokenService   = tokenService;
            _config         = config;
        }

        // ── Step 1: Validate credentials → generate OTP ───────────────────────
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

            var otp = await GenerateOtpAsync(user.Id);
            Log.Information("OTP generated for UserId: {UserId}", user.Id);

            return new LoginResponse
            {
                UserId      = user.Id,
                RoleId      = user.RoleId,
                Role        = roleName,
                Email       = user.Email,
                RequiresOtp = true,
                Otp         = otp   // TODO: remove / send via email/SMS in production
            };
        }

        // ── Step 2: Verify OTP → issue JWT ───────────────────────────────────
        public async Task<LoginResponse> VerifyOtpAsync(VerifyOtpRequest request)
        {
            var otp = await _authRepository.GetLatestOtpAsync(request.UserId);

            if (otp == null || otp.OtpCode != request.Otp)
                throw new UnauthorizedAccessException("Invalid OTP");

            if (otp.ExpiryTime < DateTime.UtcNow)
                throw new UnauthorizedAccessException("OTP has expired");

            await _authRepository.MarkOtpUsedAsync(otp);

            // Fetch user again to build token
            var user = await _authRepository.GetUserByEmail(
                (await _authRepository.GetAllUsers())
                    .First(u => u.Id == request.UserId).Email);

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

            Log.Information("OTP verified — JWT issued for UserId: {UserId}", user.Id);

            return new LoginResponse
            {
                UserId      = user.Id,
                RoleId      = user.RoleId,
                Role        = roleName,
                Email       = user.Email,
                RequiresOtp = false,
                AccessToken = token,
                TokenType   = "Bearer",
                ExpiresIn   = expiryMinutes * 60
            };
        }

        public async Task<string> GenerateOtpAsync(int userId)
        {
            var otpCode = new Random().Next(100000, 999999).ToString();

            var otpEntity = new UserOtp
            {
                UserId     = userId,
                OtpCode    = otpCode,
                ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                IsUsed     = false
            };

            await _authRepository.SaveOtpAsync(otpEntity);
            return otpCode;
        }

        public async Task CreateUserAsync(CreateUserRequest request)
            => await _authRepository.CreateUser(request);

        public async Task<List<UserSummaryResponse>> GetUsersAsync()
            => await _authRepository.GetAllUsers();

        // ── Helpers ───────────────────────────────────────────────────────────
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