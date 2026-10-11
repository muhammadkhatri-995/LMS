using LMS.API.Common;
using LMS.API.DTOs;
namespace LMS.API.Interfaces
{
    public interface IAuthService
    {
        Task<ServiceResult<UserResponseDto>> RegisterAsync(RegisterRequestDto request);
        Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    }
}
