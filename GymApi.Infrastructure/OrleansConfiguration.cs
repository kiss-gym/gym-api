using System.Reflection;
using GymApi.Domain.SessionTracking;

namespace GymApi.Infrastructure;

#region Surrogates

[GenerateSerializer]
[Alias("GymApi.Infrastructure.SessionStatusSurrogate")]
// ReSharper disable once UnusedMember.Global
public enum SessionStatusSurrogate { Active, Finished }

[GenerateSerializer]
[Alias("GymApi.Infrastructure.ExercisePropertySurrogate")]
public struct ExercisePropertySurrogate
{
    [Id(0)] public string Name;
    [Id(1)] public string Value;
}

[GenerateSerializer]
[Alias("GymApi.Infrastructure.ExerciseEntrySurrogate")]
public struct ExerciseEntrySurrogate
{
    [Id(0)] public Guid Id;
    [Id(1)] public string AutoLabel;
    [Id(2)] public string? PhotoUrl;
    [Id(3)] public DateTimeOffset? StartedAt;
    [Id(4)] public DateTimeOffset? MaxEndAt;
    [Id(5)] public DateTimeOffset? RealEndAt;
    [Id(6)] public List<ExerciseProperty> Properties;
}

[GenerateSerializer]
[Alias("GymApi.Infrastructure.TrainingSessionSurrogate")]
public struct TrainingSessionSurrogate
{
    [Id(0)] public Guid Id;
    [Id(1)] public Guid UserId;
    [Id(2)] public DateTimeOffset CreatedAt;
    [Id(3)] public DateTimeOffset? FinishedAt;
    [Id(4)] public SessionStatus Status;
    [Id(5)] public Guid? InheritedFromSessionId;
    [Id(6)] public List<ExerciseEntry> Exercises;
}

#endregion

#region Converters

[RegisterConverter]
public sealed class SessionStatusConverter : IConverter<SessionStatus, SessionStatusSurrogate>
{
    public SessionStatus ConvertFromSurrogate(in SessionStatusSurrogate surrogate) => (SessionStatus)surrogate;
    public SessionStatusSurrogate ConvertToSurrogate(in SessionStatus value) => (SessionStatusSurrogate)value;
}

[RegisterConverter]
public sealed class ExercisePropertyConverter : IConverter<ExerciseProperty, ExercisePropertySurrogate>
{
    public ExerciseProperty ConvertFromSurrogate(in ExercisePropertySurrogate surrogate) => new(surrogate.Name, surrogate.Value);
    public ExercisePropertySurrogate ConvertToSurrogate(in ExerciseProperty value) => new() { Name = value.Name, Value = value.Value };
}

[RegisterConverter]
public sealed class ExerciseEntryConverter : IConverter<ExerciseEntry, ExerciseEntrySurrogate>
{
    public ExerciseEntry ConvertFromSurrogate(in ExerciseEntrySurrogate surrogate)
    {
        var entry = (ExerciseEntry)Activator.CreateInstance(typeof(ExerciseEntry), true)!;
        Set(entry, "Id", surrogate.Id);
        Set(entry, "AutoLabel", surrogate.AutoLabel);
        Set(entry, "PhotoUrl", surrogate.PhotoUrl);
        Set(entry, "StartedAt", surrogate.StartedAt);
        Set(entry, "MaxEndAt", surrogate.MaxEndAt);
        Set(entry, "RealEndAt", surrogate.RealEndAt);
        Set(entry, "Properties", surrogate.Properties.AsReadOnly());
        return entry;
    }

    public ExerciseEntrySurrogate ConvertToSurrogate(in ExerciseEntry value) => new()
    {
        Id = value.Id, AutoLabel = value.AutoLabel, PhotoUrl = value.PhotoUrl,
        StartedAt = value.StartedAt, MaxEndAt = value.MaxEndAt, RealEndAt = value.RealEndAt,
        Properties = value.Properties.ToList()
    };

    private static void Set(object obj, string name, object? val) =>
        obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(obj, val);
}

[RegisterConverter]
public sealed class TrainingSessionConverter : IConverter<TrainingSession, TrainingSessionSurrogate>
{
    public TrainingSession ConvertFromSurrogate(in TrainingSessionSurrogate surrogate)
    {
        var session = (TrainingSession)Activator.CreateInstance(typeof(TrainingSession), true)!;
        SetProp(session, "Id", surrogate.Id);
        SetProp(session, "UserId", surrogate.UserId);
        SetProp(session, "CreatedAt", surrogate.CreatedAt);
        SetProp(session, "FinishedAt", surrogate.FinishedAt);
        SetProp(session, "Status", surrogate.Status);
        SetProp(session, "InheritedFromSessionId", surrogate.InheritedFromSessionId);
        
        var list = (List<ExerciseEntry>)session.GetType().GetField("_exercises", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session)!;
        list.AddRange(surrogate.Exercises);
        
        return session;
    }

    public TrainingSessionSurrogate ConvertToSurrogate(in TrainingSession value) => new()
    {
        Id = value.Id, UserId = value.UserId, CreatedAt = value.CreatedAt,
        FinishedAt = value.FinishedAt, Status = value.Status,
        InheritedFromSessionId = value.InheritedFromSessionId,
        Exercises = value.Exercises.ToList()
    };

    private static void SetProp(object obj, string name, object? val) =>
        obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(obj, val);
}

#endregion
