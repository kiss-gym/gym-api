using GymApi.Api.Models.Requests;
using GymApi.Api.Models.Responses;
using GymApi.Domain.SessionTracking;
using Microsoft.AspNetCore.Mvc;

namespace GymApi.Api.Controllers;

/// <summary>Current Session Tracking — core subdomain.</summary>
[ApiController]
[Route("api/sessions")]
[Produces("application/json")]
public sealed class SessionTrackingController(ICurrentSessionService service) : ControllerBase
{
    /// <summary>Create a new training session, optionally inheriting exercises.</summary>
    [HttpPost]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken ct)
    {
        var session = await service.CreateSessionAsync(request.UserId, request.InheritFromSessionId, ct);
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

    /// <summary>
    /// Add a new exercise to the session and start it immediately.
    /// Any currently running exercise is auto-finished.
    /// </summary>
    [HttpPost("{sessionId:guid}/exercises")]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddExercise(
        Guid sessionId,
        [FromBody] AddExerciseRequest request,
        CancellationToken ct)
    {
        var properties = request.Properties?
            .Select(p => new ExerciseProperty(p.Name, p.Value));

        var exercise = await service.AddExerciseAsync(
            sessionId, request.AutoLabel, request.PhotoUrl, request.MaxEndAt, properties, ct);

        return StatusCode(StatusCodes.Status201Created, ExerciseResponse.From(exercise));
    }

    /// <summary>
    /// Start a pending (inherited) exercise.
    /// Any currently running exercise is auto-finished.
    /// </summary>
    [HttpPost("{sessionId:guid}/exercises/{exerciseId:guid}/start")]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartExercise(
        Guid sessionId,
        Guid exerciseId,
        [FromBody] StartExerciseRequest request,
        CancellationToken ct)
    {
        var exercise = await service.StartExerciseAsync(sessionId, exerciseId, request.MaxEndAt, ct);
        return Ok(ExerciseResponse.From(exercise));
    }

    /// <summary>Finish a running exercise.</summary>
    [HttpPost("{sessionId:guid}/exercises/{exerciseId:guid}/finish")]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FinishExercise(
        Guid sessionId,
        Guid exerciseId,
        CancellationToken ct)
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
}
