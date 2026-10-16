using Stream.Domain.Models;

namespace Stream.Domain.Models
{
    public class Artist
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? ImageUrl { get; set; } = null!;
        public ICollection<Track> Tracks { get; set; } = new List<Track>();
        public ICollection<Album> Albums { get; set; } = new List<Album>();
    }
}
