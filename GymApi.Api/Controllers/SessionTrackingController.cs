using GymApi.Api.Models.Requests;
using GymApi.Api.Models.Responses;
using GymApi.Domain.SessionTracking;
using Microsoft.AspNetCore.Mvc;

namespace GymApi.Api.Controllers;

/// <summary>Current Session Tracking — core subdomain.</summary>
[ApiController]
[Route("api/sessions")]
[Produces("application/json")]
public sealed class SessionTrackingController(ITrainingSessionLifecycleService lifecycleService) : ControllerBase
{
    /// <summary>Create a new training session, optionally inheriting exercises.</summary>
    [HttpPost]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken ct)
    {
        var session = await lifecycleService.CreateSessionAsync(request.UserId, request.InheritFromSessionId, request.Label, ct);
        return CreatedAtAction(nameof(GetSession), new { sessionId = session.Id },
            SessionResponse.From(session));
    }

    /// <summary>Get a session by ID.</summary>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid sessionId, CancellationToken ct)
    {
        var session = await lifecycleService.GetSessionAsync(sessionId, ct);
        return Ok(SessionResponse.From(session));
    }

    /// <summary>Rename a session.</summary>
    [HttpPatch("{sessionId:guid}")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RenameSession(
        Guid sessionId,
        [FromBody] RenameSessionRequest request,
        CancellationToken ct)
    {
        var session = await lifecycleService.RenameSessionAsync(sessionId, request.Label, ct);
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

        var exercise = await lifecycleService.AddExerciseAsync(
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
        var exercise = await lifecycleService.StartExerciseAsync(sessionId, exerciseId, request.MaxEndAt, ct);
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
        var exercise = await lifecycleService.FinishExerciseAsync(sessionId, exerciseId, ct);
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
        await lifecycleService.RemoveExerciseAsync(sessionId, exerciseId, ct);
        return NoContent();
    }

    /// <summary>Finish the session. Any running exercise is auto-finished first.</summary>
    [HttpPost("{sessionId:guid}/finish")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FinishSession(Guid sessionId, CancellationToken ct)
    {
        var session = await lifecycleService.FinishSessionAsync(sessionId, ct);
        return Ok(SessionResponse.From(session));
    }
    
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> DeleteSession(Guid sessionId, CancellationToken ct)
    {
        await lifecycleService.DeleteSessionAsync(sessionId, ct);
        return NoContent(); // 204 No Content
    }

    /// <summary>Get sessions with optional filtering, sorting, and pagination.</summary>
    /// <param name="userId">Filter by user ID.</param>
    /// <param name="status">Filter by session status (Active or Finished).</param>
    /// <param name="sort">
    /// Sort criteria in format 'property[:asc|desc]'. 
    /// Supported properties: finishedAt (default), createdAt.
    /// Example: finishedAt:desc
    /// </param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 10).</param>
    /// <param name="ct"></param>
    [HttpGet]
    [ProducesResponseType<PagedResponse<SessionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(
        [FromQuery] Guid? userId,
        [FromQuery] SessionStatus? status,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var sessions = await lifecycleService.GetSessionsAsync(userId, status, sort, page, pageSize, ct);
        var totalCount = await lifecycleService.GetSessionsCountAsync(userId, status, ct);

        var response = new PagedResponse<SessionResponse>(
            sessions.Select(SessionResponse.From).ToList(),
            page,
            pageSize,
            totalCount);

        return Ok(response);
    }

    /// <summary>Get active sessions with optional filtering and pagination.</summary>
    [HttpGet("active")]
    [ProducesResponseType<PagedResponse<SessionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveSessions(
        [FromQuery] Guid? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var sessions = await lifecycleService.GetSessionsAsync(userId, SessionStatus.Active, null, page, pageSize, ct);
        var totalCount = await lifecycleService.GetSessionsCountAsync(userId, SessionStatus.Active, ct);

        var response = new PagedResponse<SessionResponse>(
            sessions.Select(SessionResponse.From).ToList(),
            page,
            pageSize,
            totalCount);

        return Ok(response);
    }
}
