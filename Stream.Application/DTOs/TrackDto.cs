using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.DTOs
{
    public class TrackDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string ArtistName { get; set; } = null!;
        public TimeSpan Duration { get; set; }
        public string? ArtworkUrl { get; set; }
        public List<ProviderTrackDto> Providers { get; set; } = [];
    }
}
