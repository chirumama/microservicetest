using MicroserviceHub.API.Application.DTOs.Request;
using MicroserviceHub.API.Application.DTOs.Response;
using MicroserviceHub.API.Domain.Entities;

namespace MicroserviceHub.API.Application.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByEmail(string email);
        Task CreateUser(CreateUserRequest request);
        Task<List<UserSummaryResponse>> GetAllUsers();
        Task SetUserActive(int userId, bool isActive);
        Task UpdatePasswordAsync(int userId, string newPasswordHash);

        // OTP methods
        Task SaveOtpAsync(UserOtp otp);
        Task<UserOtp?> GetLatestOtpAsync(int userId);
        Task MarkOtpUsedAsync(UserOtp otp);
    }
}