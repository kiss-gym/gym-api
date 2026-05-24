using GymApi.Api.Models.Requests;
using GymApi.Api.Models.Responses;
using GymApi.Domain.SessionTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymApi.Api.Controllers;

/// <summary>Current Session Tracking — core subdomain.</summary>
[ApiController]
[Route("api/sessions")]
[Produces("application/json")]
[Authorize]
public sealed class SessionTrackingController(ITrainingSessionService service) : ControllerBase
{
    // ── Sessions ─────────────────────────────────────────────────────────────

    /// <summary>Create a new training session, optionally inheriting exercises.</summary>
    [HttpPost]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreateSessionRequest request, CancellationToken ct)
    {
        var session = await service.CreateSessionAsync(request.InheritFromSessionId, request.Label, ct);
        return CreatedAtAction(nameof(GetSession), new { sessionId = session.Id },
            SessionResponse.From(session));
    }

    /// <summary>Get a session by ID.</summary>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid sessionId, CancellationToken ct)
    {
        var session = await service.GetSessionAsync(sessionId, ct);
        return Ok(SessionResponse.From(session));
    }

    /// <summary>Rename a session.</summary>
    [HttpPatch("{sessionId:guid}")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RenameSession(
        Guid sessionId, [FromBody] RenameSessionRequest request, CancellationToken ct)
    {
        var session = await service.RenameSessionAsync(sessionId, request.Label, ct);
        return Ok(SessionResponse.From(session));
    }

    /// <summary>Finish the session. Any running exercise is auto-finished first.</summary>
    [HttpPost("{sessionId:guid}/finish")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FinishSession(Guid sessionId, CancellationToken ct)
    {
        var session = await service.FinishSessionAsync(sessionId, ct);
        return Ok(SessionResponse.From(session));
    }

    /// <summary>Delete a session.</summary>
    [HttpDelete("{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(Guid sessionId, CancellationToken ct)
    {
        await service.DeleteSessionAsync(sessionId, ct);
        return NoContent();
    }

    /// <summary>Get sessions with optional filtering, sorting, and pagination.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<SessionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(
        [FromQuery] SessionStatus? status,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var sessions = await service.GetSessionsAsync(status, sort, page, pageSize, ct);
        var totalCount = await service.GetSessionsCountAsync(status, ct);
        return Ok(new PagedResponse<SessionResponse>(
            sessions.Select(SessionResponse.From).ToList(), page, pageSize, totalCount));
    }

    /// <summary>Get active sessions with optional pagination.</summary>
    [HttpGet("active")]
    [ProducesResponseType<PagedResponse<SessionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveSessions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var sessions = await service.GetSessionsAsync(SessionStatus.Active, null, page, pageSize, ct);
        var totalCount = await service.GetSessionsCountAsync(SessionStatus.Active, ct);
        return Ok(new PagedResponse<SessionResponse>(
            sessions.Select(SessionResponse.From).ToList(), page, pageSize, totalCount));
    }

    // ── Exercises ─────────────────────────────────────────────────────────────

    /// <summary>Add a new exercise to the session.</summary>
    [HttpPost("{sessionId:guid}/exercises")]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddExercise(
        Guid sessionId, [FromBody] AddExerciseRequest request, CancellationToken ct)
    {
        var properties = request.Properties?.Select(p => new ExerciseProperty(p.Name, p.Value));
        var exercise = await service.AddExerciseAsync(sessionId, request.AutoLabel, request.PhotoUrl, properties, ct);
        return StatusCode(StatusCodes.Status201Created, ExerciseResponse.From(exercise));
    }

    /// <summary>Update an exercise.</summary>
    [HttpPatch("{sessionId:guid}/exercises/{exerciseId:guid}")]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateExercise(
        Guid sessionId, Guid exerciseId, [FromBody] UpdateExerciseRequest request, CancellationToken ct)
    {
        var properties = request.Properties?.Select(p => new ExerciseProperty(p.Name, p.Value));
        var exercise = await service.UpdateExerciseAsync(
            sessionId, exerciseId, request.AutoLabel, request.PhotoUrl, properties, ct);
        return Ok(ExerciseResponse.From(exercise));
    }

    /// <summary>Start a pending exercise.</summary>
    [HttpPost("{sessionId:guid}/exercises/{exerciseId:guid}/start")]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartExercise(
        Guid sessionId, Guid exerciseId, [FromBody] StartExerciseRequest request, CancellationToken ct)
    {
        var exercise = await service.StartExerciseAsync(sessionId, exerciseId, ct);
        return Ok(ExerciseResponse.From(exercise));
    }

    /// <summary>Finish a running exercise.</summary>
    [HttpPost("{sessionId:guid}/exercises/{exerciseId:guid}/finish")]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FinishExercise(
        Guid sessionId, Guid exerciseId, CancellationToken ct)
    {
        var exercise = await service.FinishExerciseAsync(sessionId, exerciseId, ct);
        return Ok(ExerciseResponse.From(exercise));
    }

    /// <summary>Remove an exercise from an active session.</summary>
    [HttpDelete("{sessionId:guid}/exercises/{exerciseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveExercise(
        Guid sessionId, Guid exerciseId, CancellationToken ct)
    {
        await service.RemoveExerciseAsync(sessionId, exerciseId, ct);
        return NoContent();
    }

    // ── Sets ──────────────────────────────────────────────────────────────────

    /// <summary>Get all sets for an exercise.</summary>
    [HttpGet("{sessionId:guid}/exercises/{exerciseId:guid}/sets")]
    [ProducesResponseType<IReadOnlyList<ExerciseSetResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSets(
        Guid sessionId, Guid exerciseId, CancellationToken ct)
    {
        var session = await service.GetSessionAsync(sessionId, ct);
        var exercise = session.Exercises.FirstOrDefault(e => e.Id == exerciseId)
            ?? throw new KeyNotFoundException($"Exercise {exerciseId} not found.");
        return Ok(exercise.SortedSets.Select(ExerciseSetResponse.From).ToList());
    }

    /// <summary>Add a new set to an exercise.</summary>
    [HttpPost("{sessionId:guid}/exercises/{exerciseId:guid}/sets")]
    [ProducesResponseType<ExerciseSetResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddSet(
        Guid sessionId, Guid exerciseId, [FromBody] AddSetRequest request, CancellationToken ct)
    {
        var set = await service.AddSetAsync(sessionId, exerciseId, request.Weight, request.Repetitions, ct);
        return StatusCode(StatusCodes.Status201Created, ExerciseSetResponse.From(set));
    }

    /// <summary>Add a copy of the last set to an exercise.</summary>
    [HttpPost("{sessionId:guid}/exercises/{exerciseId:guid}/sets/copy-last")]
    [ProducesResponseType<ExerciseSetResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddCopyOfLastSet(
        Guid sessionId, Guid exerciseId, CancellationToken ct)
    {
        var set = await service.AddCopyOfLastSetAsync(sessionId, exerciseId, ct);
        return StatusCode(StatusCodes.Status201Created, ExerciseSetResponse.From(set));
    }

    /// <summary>Update weight and repetitions of a set.</summary>
    [HttpPatch("{sessionId:guid}/exercises/{exerciseId:guid}/sets/{setId:guid}")]
    [ProducesResponseType<ExerciseSetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSet(
        Guid sessionId, Guid exerciseId, Guid setId,
        [FromBody] UpdateSetRequest request, CancellationToken ct)
    {
        var set = await service.UpdateSetAsync(sessionId, exerciseId, setId, request.Weight, request.Repetitions, ct);
        return Ok(ExerciseSetResponse.From(set));
    }

    /// <summary>Delete a set.</summary>
    [HttpDelete("{sessionId:guid}/exercises/{exerciseId:guid}/sets/{setId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteSet(
        Guid sessionId, Guid exerciseId, Guid setId, CancellationToken ct)
    {
        await service.DeleteSetAsync(sessionId, exerciseId, setId, ct);
        return NoContent();
    }

    /// <summary>Mark a set as completed.</summary>
    [HttpPost("{sessionId:guid}/exercises/{exerciseId:guid}/sets/{setId:guid}/complete")]
    [ProducesResponseType<ExerciseSetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteSet(
        Guid sessionId, Guid exerciseId, Guid setId, CancellationToken ct)
    {
        var set = await service.CompleteSetAsync(sessionId, exerciseId, setId, ct);
        return Ok(ExerciseSetResponse.From(set));
    }

    /// <summary>Mark a set as not completed.</summary>
    [HttpPost("{sessionId:guid}/exercises/{exerciseId:guid}/sets/{setId:guid}/uncomplete")]
    [ProducesResponseType<ExerciseSetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UnCompleteSet(
        Guid sessionId, Guid exerciseId, Guid setId, CancellationToken ct)
    {
        var set = await service.UnCompleteSetAsync(sessionId, exerciseId, setId, ct);
        return Ok(ExerciseSetResponse.From(set));
    }
}
