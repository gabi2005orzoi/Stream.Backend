using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stream.Domain.Models;

namespace Stream.Backend.Persistence.Configurations
{
    public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
    {
        public void Configure(EntityTypeBuilder<Favorite> builder)
        {
            builder.ToTable("favorites");

            builder.HasKey(f => new
            {
                f.UserId,
                f.TrackId
            });

            builder.Property(f => f.CreatedAt)
                .IsRequired();

            builder.HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(f => f.Track)
                .WithMany()
                .HasForeignKey(f => f.TrackId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
