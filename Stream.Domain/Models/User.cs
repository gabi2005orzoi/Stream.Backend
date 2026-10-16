namespace Stream.Domain.Models
{
    public class User
    {
        public Guid Id { get; set; }

        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
        public ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
        public ICollection<Favorite> Favorites { get; set;} = new List<Favorite>();
        public ICollection<ListeningHistory> ListeningHistory { get; set; } = new List<ListeningHistory>();

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
