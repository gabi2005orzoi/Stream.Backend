namespace Stream.Domain.Models
{
    public class ProviderTrack
    {
        public Guid Id { get; set; }

        public Guid TrackId { get; set; }
        public Track Track { get; set; } = null!;

        public string Provider { get; set; } = null!;
        public string ExternalId { get; set; } = null!;

        public string? ExternalUrl { get; set; }

        public bool IsPlayable { get; set; }

        public DateTime? LastCheckedAt { get; set; }
    }
}
