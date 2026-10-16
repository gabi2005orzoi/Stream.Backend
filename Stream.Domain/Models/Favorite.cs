namespace Stream.Domain.Models
{
    public class Favorite
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid TrackId { get; set; }
        public Track Track { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
