using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;

namespace MicroserviceHub.API.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest Request);
        Task CreateUserAsync(CreateUserRequest request);
        Task<List<UserSummaryResponse>> GetUsersAsync();
    }
}
