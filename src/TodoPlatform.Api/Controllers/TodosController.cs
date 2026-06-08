using Microsoft.AspNetCore.Mvc;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Services;

namespace TodoPlatform.Api.Controllers;

/// <summary>
/// Todo CRUD endpoints aligned with contracts/openapi.yaml.
/// </summary>
[ApiController]
[Route("api/todos")]
[Produces("application/json")]
public class TodosController(ITodoService todoService) : ControllerBase
{
    /// <summary>
    /// List todos for a user.
    /// </summary>
    /// <param name="userId">Owner user id.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TodoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TodoDto>>> List(
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            throw ValidationException.ForField("userId", "Query parameter 'userId' is required.");

        var todos = await todoService.ListByUserAsync(userId, cancellationToken);
        return Ok(todos);
    }

    /// <summary>
    /// Get a single todo by id.
    /// </summary>
    /// <param name="id">Todo id.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TodoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var todo = await todoService.GetByIdAsync(id, cancellationToken);
        if (todo is null)
            throw new NotFoundException($"Todo '{id}' was not found.");

        return Ok(todo);
    }

    /// <summary>
    /// Create a new todo.
    /// </summary>
    /// <param name="request">Todo payload.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost]
    [ProducesResponseType(typeof(TodoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TodoDto>> Create(
        [FromBody] CreateTodoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw ValidationException.ForField("title", "Title is required.");

        if (request.UserId == Guid.Empty)
            throw ValidationException.ForField("userId", "UserId is required.");

        try
        {
            var todo = await todoService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = todo.Id }, todo);
        }
        catch (ArgumentException ex)
        {
            throw ValidationException.ForField("title", ex.Message);
        }
    }

    /// <summary>
    /// Partially update a todo.
    /// </summary>
    /// <param name="id">Todo id.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(TodoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoDto>> Update(
        Guid id,
        [FromBody] UpdateTodoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var todo = await todoService.UpdateAsync(id, request, cancellationToken);
            if (todo is null)
                throw new NotFoundException($"Todo '{id}' was not found.");

            return Ok(todo);
        }
        catch (ArgumentException ex)
        {
            throw ValidationException.ForField("status", ex.Message);
        }
    }

    /// <summary>
    /// Delete a todo.
    /// </summary>
    /// <param name="id">Todo id.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await todoService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            throw new NotFoundException($"Todo '{id}' was not found.");

        return NoContent();
    }
}
