using GymApi.Domain.SessionTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApi.Infrastructure.SessionTracking;

internal sealed class ExerciseSetConfiguration : IEntityTypeConfiguration<ExerciseSet>
{
    public void Configure(EntityTypeBuilder<ExerciseSet> builder)
    {
        builder.ToTable("exercise_sets");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        // Shadow FK to exercise_entries — not exposed on domain model
        builder.Property<Guid>("exercise_id")
            .HasColumnName("exercise_id")
            .IsRequired();

        builder.Property(s => s.SetNumber)
            .HasColumnName("set_number")
            .IsRequired();

        builder.Property(s => s.IsCompleted)
            .HasColumnName("is_completed")
            .IsRequired();

        builder.Property(s => s.Weight)
            .HasColumnName("weight")
            .HasColumnType("numeric(6,2)");

        builder.Property(s => s.Repetitions)
            .HasColumnName("repetitions");
    }
}
