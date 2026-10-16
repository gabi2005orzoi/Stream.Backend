using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Stream.Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }

        public string RefreshToken { get; set; } = null!;
    }
}
