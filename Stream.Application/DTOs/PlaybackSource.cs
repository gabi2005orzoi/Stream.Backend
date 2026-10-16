using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.DTOs
{
    public class PlaybackSource
    {
        public string Provider { get; set; } = null!;
        public string Url { get; set; } = null!;
        public string? MimeType { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
