using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stream.Domain.Models;

namespace Stream.Backend.Persistence.Configurations
{
    public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
    {
        public void Configure(EntityTypeBuilder<Artist> builder)
        {
            builder.ToTable("artists");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(a => a.ImageUrl)
                .HasMaxLength(255);

            builder.HasIndex(a => a.Name);
        }
    }
}
