using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stream.Domain.Models;

namespace Stream.Backend.Persistence.Configurations
{
    public class ListeningHistoryConfiguration : IEntityTypeConfiguration<ListeningHistory>
    {
        public void Configure(EntityTypeBuilder<ListeningHistory> builder)
        {
            builder.ToTable("listening_history");

            builder.HasKey(lh => lh.Id);

            builder.Property(lh => lh.PlayedAt)
                .IsRequired();

            builder.Property(lh => lh.Completed)
                .IsRequired();

            builder.HasOne(lh => lh.User)
                .WithMany(u => u.ListeningHistory)
                .HasForeignKey(lh => lh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(lh => lh.Track)
                .WithMany()
                .HasForeignKey(lh => lh.TrackId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(lh => new
            {
                lh.UserId,
                lh.PlayedAt
            });
        }
    }
}
