using GymApi.Domain.SessionTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApi.Infrastructure.SessionTracking;

internal sealed class ExerciseEntryConfiguration : IEntityTypeConfiguration<ExerciseEntry>
{
    public void Configure(EntityTypeBuilder<ExerciseEntry> builder)
    {
        builder.ToTable("exercise_entries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property<Guid>("session_id")
            .HasColumnName("session_id")
            .IsRequired();

        builder.Property(e => e.AutoLabel)
            .HasColumnName("auto_label")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.PhotoUrl)
            .HasColumnName("photo_url");

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at");

        builder.Property(e => e.RealEndAt)
            .HasColumnName("real_end_at");

        builder.Property(e => e.Properties)
            .HasColumnName("properties")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasMany(e => e.SortedSets)
            .WithOne()
            .HasForeignKey("exercise_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.SortedSets)
            .HasField("_sets")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
