using System;

namespace Stream.Application.DTOs.PlaylistDTOs
{
    public class PlaylistTrackDto
    {
        public Guid TrackId { get; set; }
        public string Title { get; set; } = null!;
        public string ArtistName { get; set; } = null!;
        public TimeSpan Duration { get; set; }
        public string? ArtworkUrl { get; set; }
        public int Position { get; set; }
        public DateTime AddedAt { get; set; }
    }
}
