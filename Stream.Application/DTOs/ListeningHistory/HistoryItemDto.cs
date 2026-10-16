using System;
using System.Collections.Generic;
using System.Text;

namespace Stream.Application.DTOs.ListeningHistory
{
    public class HistoryItemDto
    {
        public Guid Id { get; set; }
        public Guid TrackId { get; set; }
        public string Title { get; set; } = null!;
        public string ArtistName { get; set; } = null!;
        public string? ArtworkUrl { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime PlayedAt { get; set; }
        public bool Completed { get; set; }
    }
}
