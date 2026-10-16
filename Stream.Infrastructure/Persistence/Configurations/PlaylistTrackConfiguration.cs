using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stream.Domain.Models;

namespace Stream.Backend.Persistence.Configurations
{
    public class PlaylistTrackConfiguration : IEntityTypeConfiguration<PlaylistTrack>
    {
        public void Configure(EntityTypeBuilder<PlaylistTrack> builder)
        {
            builder.ToTable("playlist_tracks");

            builder.HasKey(pt => new
            {
                pt.PlaylistId,
                pt.TrackId,
                pt.Position
            });

            builder.Property(pt => pt.Position)
                .IsRequired();

            builder.Property(pt => pt.AddedAt)
                .IsRequired();

            builder.HasOne(pt => pt.Playlist)
                .WithMany(p => p.Tracks)
                .HasForeignKey(pt => pt.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pt => pt.Track)
                .WithMany()
                .HasForeignKey(pt => pt.TrackId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(pt => new
            {
                pt.PlaylistId,
                pt.Position
            });
        }
    }
}
