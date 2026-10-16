
namespace Stream.Domain.Models
{
    public class ListeningHistory
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid TrackId { get; set; }
        public Track Track { get; set; } = null!;

        public DateTime PlayedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Completed { get; set; }
    }
}
