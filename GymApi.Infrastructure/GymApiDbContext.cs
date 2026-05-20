using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using GymApi.Infrastructure.SessionTracking;
using GymApi.Infrastructure.UserManagement;
using Microsoft.EntityFrameworkCore;

namespace GymApi.Infrastructure;

public sealed class GymApiDbContext(DbContextOptions<GymApiDbContext> options) : DbContext(options)
{
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<ExerciseEntry> ExerciseEntries => Set<ExerciseEntry>();
    public DbSet<ExerciseSet> ExerciseSets => Set<ExerciseSet>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TrainingSessionConfiguration());
        modelBuilder.ApplyConfiguration(new ExerciseEntryConfiguration());
        modelBuilder.ApplyConfiguration(new ExerciseSetConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}
