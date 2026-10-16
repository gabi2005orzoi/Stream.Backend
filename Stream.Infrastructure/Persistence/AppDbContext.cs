using Microsoft.EntityFrameworkCore;
using Stream.Application.Interfaces;
using Stream.Domain.Models;

namespace Stream.Backend.Persistence
{
    public class AppDbContext: DbContext, IUnitOfWork
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Artist> Artists => Set<Artist>();
        public DbSet<Album> Albums => Set<Album>();
        public DbSet<Track> Tracks => Set<Track>();
        public DbSet<ProviderTrack> ProviderTracks => Set<ProviderTrack>();
        public DbSet<Playlist> Playlists => Set<Playlist>();
        public DbSet<PlaylistTrack> PlaylistTracks => Set<PlaylistTrack>();
        public DbSet<Favorite> Favorites => Set<Favorite>();
        public DbSet<ListeningHistory> ListeningHistory => Set<ListeningHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly);
        }
    }
}
