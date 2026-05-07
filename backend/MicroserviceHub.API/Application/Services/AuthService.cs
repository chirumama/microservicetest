using System.Net;
using System.Net.Mail;
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

        // ── Step 1: Validate credentials → generate & email OTP ──────────────
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

            var roleName = MapRole(user.RoleId);
            var otp      = await GenerateOtpAsync(user.Id);

            await SendOtpEmailAsync(user.Email, otp, "Your Login OTP");

            Log.Information("OTP generated and emailed for UserId: {UserId}", user.Id);

            // OTP is NOT returned in the response — it goes to email only
            return new LoginResponse
            {
                UserId      = user.Id,
                RoleId      = user.RoleId,
                Role        = roleName,
                Email       = user.Email,
                RequiresOtp = true,
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

            var user = await _authRepository.GetUserByEmail(
                (await _authRepository.GetAllUsers())
                    .First(u => u.Id == request.UserId).Email);

            var roleName = MapRole(user.RoleId);

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

        // ── Forgot password: generate OTP and email it ────────────────────────
        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _authRepository.GetUserByEmail(email);

            // Don't reveal whether email exists — silently return if not found
            if (user == null)
            {
                Log.Warning("Forgot-password requested for unknown email: {Email}", email);
                return;
            }

            var otp = await GenerateOtpAsync(user.Id);
            await SendOtpEmailAsync(user.Email, otp, "Password Reset OTP");

            Log.Information("Password-reset OTP emailed for UserId: {UserId}", user.Id);
        }

        // ── Forgot password: verify OTP and update password ───────────────────
        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _authRepository.GetUserByEmail(request.Email);
            if (user == null)
                throw new UnauthorizedAccessException("Invalid request");

            var otp = await _authRepository.GetLatestOtpAsync(user.Id);

            if (otp == null || otp.OtpCode != request.Otp)
                throw new UnauthorizedAccessException("Invalid OTP");

            if (otp.ExpiryTime < DateTime.UtcNow)
                throw new UnauthorizedAccessException("OTP has expired");

            await _authRepository.MarkOtpUsedAsync(otp);

            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _authRepository.UpdatePasswordAsync(user.Id, newHash);

            Log.Information("Password reset successfully for UserId: {UserId}", user.Id);
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

        // ── Email helper ──────────────────────────────────────────────────────
        private async Task SendOtpEmailAsync(string toEmail, string otp, string subject)
        {
            var smtpHost     = _config["Email:SmtpHost"]     ?? throw new InvalidOperationException("Email:SmtpHost not configured");
            var smtpPortStr  = _config["Email:SmtpPort"]     ?? "587";
            var smtpUser     = _config["Email:SmtpUser"]     ?? throw new InvalidOperationException("Email:SmtpUser not configured");
            var smtpPassword = _config["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword not configured");
            var fromEmail    = _config["Email:FromAddress"]  ?? smtpUser;
            var fromName     = _config["Email:FromName"]     ?? "MicroserviceHub";

            var smtpPort = int.Parse(smtpPortStr);

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials    = new NetworkCredential(smtpUser, smtpPassword),
                EnableSsl      = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };

            var body = $@"
<html><body>
  <p>Hi,</p>
  <p>Your OTP for <strong>{subject}</strong> is:</p>
  <h2 style=""letter-spacing:8px;color:#2f5ec3"">{otp}</h2>
  <p>This OTP is valid for <strong>5 minutes</strong>. Do not share it with anyone.</p>
  <p>If you did not request this, please ignore this email.</p>
  <br/>
  <p>– MicroserviceHub Team</p>
</body></html>";

            var message = new MailMessage
            {
                From       = new MailAddress(fromEmail, fromName),
                Subject    = subject,
                Body       = body,
                IsBodyHtml = true,
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            Log.Information("OTP email sent to {Email}", toEmail);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static string MapRole(int roleId) => roleId switch
        {
            1 => "User",
            2 => "Admin",
            3 => "SuperAdmin",
            _ => "User"
        };

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