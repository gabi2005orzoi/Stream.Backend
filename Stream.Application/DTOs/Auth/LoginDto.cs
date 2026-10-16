using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.DTOs.Auth
{
    public class LoginDto
    {
        public string EmailOrUsername { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
