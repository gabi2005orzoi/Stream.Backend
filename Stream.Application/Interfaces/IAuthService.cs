using Stream.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken);
        Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task RevokeTokenAsync(Guid userId, CancellationToken cancellationToken);
        Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task<UserDto?> GetMeAsync(Guid userId, CancellationToken cancellationToken);
    }
}
