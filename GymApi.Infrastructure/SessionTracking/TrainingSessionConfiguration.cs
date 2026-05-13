using GymApi.Domain.SessionTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymApi.Infrastructure.SessionTracking;

internal sealed class TrainingSessionConfiguration : IEntityTypeConfiguration<TrainingSession>
{
    public void Configure(EntityTypeBuilder<TrainingSession> builder)
    {
        builder.ToTable("training_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.Label)
            .HasColumnName("label")
            .HasMaxLength(200);

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.FinishedAt)
            .HasColumnName("finished_at");

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()   // stores "Active" / "Finished" as text
            .IsRequired();

        builder.Property(s => s.InheritedFromSessionId)
            .HasColumnName("inherited_from_session_id");

        // Map the private backing field _exercises
        // EF reads/writes _exercises directly, Exercises property is the read-only view
        builder.HasMany(s => s.Exercises)
            .WithOne()
            .HasForeignKey("session_id")   // shadow FK — not on ExerciseEntry domain model
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Exercises)
            .HasField("_exercises")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
