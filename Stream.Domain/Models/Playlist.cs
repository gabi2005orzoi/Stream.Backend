namespace Stream.Domain.Models
{
    public class Playlist
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<PlaylistTrack> Tracks { get; set; } = new List<PlaylistTrack>();
    }
}
