using Stream.Domain.Models;

namespace Stream.Domain.Models
{
    public class Track
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;

        public Guid ArtistId { get; set; }
        public Artist Artist { get; set; } = null!;

        public Guid? AlbumId { get; set; }
        public Album? Album { get; set; }

        public TimeSpan Duration { get; set; }
        public string? ArtworkUrl { get; set;  }
        public ICollection<ProviderTrack> ProviderTracks { get; set; } = new List<ProviderTrack>();
    }
}
