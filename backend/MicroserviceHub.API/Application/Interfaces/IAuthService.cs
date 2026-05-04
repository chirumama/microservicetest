using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;

namespace MicroserviceHub.API.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task CreateUserAsync(CreateUserRequest request);
        Task<List<UserSummaryResponse>> GetUsersAsync();

        Task<string> GenerateOtpAsync(int userId);
        Task<LoginResponse> VerifyOtpAsync(VerifyOtpRequest request);  // ← this line is what's missing
    }
}