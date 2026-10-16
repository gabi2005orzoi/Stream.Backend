using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stream.Application.DTOs.Auth;
using Stream.Application.Interfaces;

namespace Stream.Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;
        private readonly ICurrentUserService currentUserService;

        public AuthController(IAuthService authService, ICurrentUserService currentUserService)
        {
            this.authService = authService;
            this.currentUserService = currentUserService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await authService.RegisterAsync(dto, cancellationToken);

                SetRefreshTokenCookie(result.RefreshToken, DateTime.UtcNow.AddDays(30));

                return Ok(result);
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await authService.LoginAsync(dto, cancellationToken);

                SetRefreshTokenCookie(result.RefreshToken, DateTime.UtcNow.AddDays(30));

                return Ok(result);
            }
            catch(UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(
            [FromBody(EmptyBodyBehavior =
                Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] RefreshTokenDto? dto,
            CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies["refreshToken"] ?? dto?.RefreshToken;
            if(string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new {message = "No refresh token provided"});

            try
            {
                var result = await authService.RefreshTokenAsync(refreshToken, cancellationToken);

                SetRefreshTokenCookie(result.RefreshToken, DateTime.UtcNow.AddDays(30));

                return Ok(result);
            }
            catch (OperationCanceledException)
            {   
                return StatusCode(499);
            }
            catch(UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        {
            var user = await authService.GetMeAsync(currentUserService.UserId, cancellationToken);
            if (user == null)
                return Unauthorized(new { message = "User not found" });

            return Ok(user);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            [FromBody(EmptyBodyBehavior =
                Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] RefreshTokenDto? dto,
            CancellationToken cancellationToken)
        {
            if (currentUserService.UserId != Guid.Empty)
            {
                await authService.RevokeTokenAsync(currentUserService.UserId, cancellationToken);
            }
            else
            {
                var refreshToken = Request.Cookies["refreshToken"] ?? dto?.RefreshToken;
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await authService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
                }
            }

            DeleteRefreshTokenCookie();
            return Ok(new { message = "Logged out successfully" });
        }

        private void SetRefreshTokenCookie(string refreshToken, DateTime expires)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = expires
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }

        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax
            });
        }
    }
}
