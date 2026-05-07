namespace MicroserviceHub.API.Application.DTOs.Response
{
    /// <summary>
    /// Returned by POST /v1.0.1/auth/login
    /// Step 1: Always returns UserId, RoleId, Role, Email, RequiresOtp = true.
    /// Client stores these temporarily and calls POST /v1.0.1/auth/verify-otp.
    /// Step 2 (verify-otp): returns the JWT AccessToken for subsequent requests.
    /// </summary>
    public class LoginResponse
    {
        // ── Step-1 fields (always present) ──────────────────────────────────
        public int    UserId      { get; set; }
        public int    RoleId      { get; set; }
        public string Role        { get; set; } = string.Empty;
        public string Email       { get; set; } = string.Empty;
        public bool   RequiresOtp { get; set; }

        // ── Step-2 fields (populated after OTP verified) ─────────────────────
        public string? AccessToken { get; set; }
        public string  TokenType   { get; set; } = "Bearer";
        public int     ExpiresIn   { get; set; }
    }
}