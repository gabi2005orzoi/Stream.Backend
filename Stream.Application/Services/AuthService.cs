using Microsoft.Extensions.Configuration;
using Stream.Application.DTOs.Auth;
using Stream.Application.Interfaces;
using Stream.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.Services
{
    public class AuthService: IAuthService
    {
        private readonly IUserRepository userRepository;
        private readonly IPasswordHasher passwordHasher;
        private readonly IJwtTokenGenerator jwtTokenGenerator;
        private readonly IConfiguration configuration;

        public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator, IConfiguration configuration)
        {
            this.userRepository = userRepository;
            this.passwordHasher = passwordHasher;
            this.jwtTokenGenerator = jwtTokenGenerator;
            this.configuration = configuration;
        }

        public async Task<UserDto?> GetMeAsync(Guid userId, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return null;

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByEmailAsync(dto.EmailOrUsername, cancellationToken);
            if (user == null)
                user = await userRepository.GetByUsernameAsync(dto.EmailOrUsername, cancellationToken);

            if (user == null || !passwordHasher.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Incorrect credentials");

            var refreshTokenDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");

            user.RefreshToken = jwtTokenGenerator.GenerateRefreshToken();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenDays);
            await userRepository.UpdateAsync(user, cancellationToken);

            var accessToken = jwtTokenGenerator.GenerateAccessToken(user);
            var expirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");

            return new AuthResponseDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                AccessToken = accessToken,
                RefreshToken = user.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByRefreshTokenAsync(refreshToken, cancellationToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Invalid or expired refresh token. Please login again!");

            var refreshTokenDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");
            user.RefreshToken = jwtTokenGenerator.GenerateRefreshToken();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenDays);
            await userRepository.UpdateAsync(user, cancellationToken);

            var accessToken = jwtTokenGenerator.GenerateAccessToken(user);
            var expirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");

            return new AuthResponseDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                AccessToken = accessToken,
                RefreshToken = user.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken)
        {
            if (await userRepository.ExistsByEmailAsync(dto.Email, cancellationToken))
                throw new InvalidOperationException("Email already registered");

            if (await userRepository.ExistsByUsernameAsync(dto.Username, cancellationToken))
                throw new InvalidOperationException("Username already used");

            var refreshTokenDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "30");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHasher.Hash(dto.Password),
                CreatedAt = DateTime.UtcNow,
                RefreshToken = jwtTokenGenerator.GenerateRefreshToken(),
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenDays)
            };

            await userRepository.AddAsync(user, cancellationToken);

            var accessToken = jwtTokenGenerator.GenerateAccessToken(user);
            var expirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");

            return new AuthResponseDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                AccessToken = accessToken,
                RefreshToken = user.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };
        }

        public async Task RevokeTokenAsync(Guid userId, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if(user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await userRepository.UpdateAsync(user, cancellationToken);
            }
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByRefreshTokenAsync(refreshToken, cancellationToken);
            if(user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await userRepository.UpdateAsync(user, cancellationToken);
            }
        }
    }
}
