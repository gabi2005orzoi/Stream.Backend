using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.DTOs
{
    public class TrackCandidate
    {
        public string Provider { get; set; } = null!;
        public string ExternalId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Artist { get; set; } = null!;
        public string? Album { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? ArtworkUrl { get; set; }
        public string? ExternalUrl { get; set; }
    }
}
