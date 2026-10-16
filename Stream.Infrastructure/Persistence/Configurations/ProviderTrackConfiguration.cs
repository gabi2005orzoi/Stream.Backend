using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stream.Domain.Models;

namespace Stream.Backend.Persistence.Configurations
{
    public class ProviderTrackConfiguration : IEntityTypeConfiguration<ProviderTrack>
    {
        public void Configure(EntityTypeBuilder<ProviderTrack> builder)
        {
            builder.ToTable("provider_tracks");

            builder.HasKey(pt => pt.Id);

            builder.Property(pt => pt.Provider)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(pt => pt.ExternalId)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(pt => pt.ExternalUrl)
                .IsRequired(false)
                .HasMaxLength(2048);

            builder.Property(pt => pt.IsPlayable)
                .IsRequired();

            builder.HasOne(pt => pt.Track)
                .WithMany(t => t.ProviderTracks)
                .HasForeignKey(t => t.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(pt => new
            {
                pt.Provider,
                pt.ExternalId
            })
            .IsUnique();

            builder.HasIndex(pt => pt.TrackId);

        }
    }
}
