using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.DTOs.PlaylistDTOs
{
    public class PlaylistSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int TracksCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> ArtworkUrls { get; set; } = new();
    }
}
