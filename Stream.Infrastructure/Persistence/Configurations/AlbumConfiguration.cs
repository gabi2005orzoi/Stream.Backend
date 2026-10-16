using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stream.Domain.Models;

namespace Stream.Backend.Persistence.Configurations
{
    public class AlbumConfiguration : IEntityTypeConfiguration<Album>
    {
        public void Configure(EntityTypeBuilder<Album> builder)
        {
            builder.ToTable("albums");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(a => a.ArtworkUrl)
                .HasMaxLength(2048);

            builder.Property(a => a.ReleseDate)
                .IsRequired(false);

            builder.HasOne(a => a.Artist)
                .WithMany(a => a.Albums)
                .HasForeignKey(a => a.ArtistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => new
            {
                a.ArtistId,
                a.Title
            });

        }
    }
}
