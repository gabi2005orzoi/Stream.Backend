using Stream.Domain.Models;

namespace Stream.Domain.Models
{
    public class Album
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        
        public Guid ArtistId { get; set; }
        public Artist Artist { get; set; } = null!;

        public string? ArtworkUrl { get; set; }


        public DateTime? ReleseDate { get; set; }
        
        public ICollection<Track> Tracks { get; set; } = new List<Track>();
    }
}
