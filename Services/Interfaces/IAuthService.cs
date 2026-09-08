using PotyInternosAPI.DTOs.Auth;

namespace PotyInternosAPI.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
}
