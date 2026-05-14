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

        // Shadow FK — defined on TrainingSessionConfiguration, declared here for column naming
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

        // ExerciseProperty is a sealed record — serialized as a jsonb array.
        // EF 10 maps IReadOnlyList<record> to JSON natively via a primitive collection.
        builder.Property(e => e.Properties)
            .HasColumnName("properties")
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
