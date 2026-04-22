using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;

namespace MicroserviceHub.API.Application.Interfaces
{
    public interface IOAuthService
    {
        Task<TokenResponse> IssueApiTokenAsync(ClientCredentialsRequest request);
    }
}