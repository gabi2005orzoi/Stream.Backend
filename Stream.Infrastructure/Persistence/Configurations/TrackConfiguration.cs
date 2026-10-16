using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stream.Domain.Models;

namespace Stream.Backend.Persistence.Configurations
{
    public class TrackConfiguration : IEntityTypeConfiguration<Track>
    {
        public void Configure(EntityTypeBuilder<Track> builder)
        {
            builder.ToTable("tracks");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(t => t.ArtworkUrl)
                .HasMaxLength(2048);

            builder.Property(t => t.Duration)
                .IsRequired();

            builder.HasOne(t => t.Artist)
                .WithMany(a => a.Tracks)
                .HasForeignKey(t => t.ArtistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Album)
                .WithMany(a => a.Tracks)
                .HasForeignKey(t => t.AlbumId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(t => t.Title);
            builder.HasIndex(t => t.ArtistId);
            builder.HasIndex(t => t.AlbumId);
        }
    }
}
