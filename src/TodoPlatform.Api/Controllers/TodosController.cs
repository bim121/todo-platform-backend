using MediatR;
using Microsoft.AspNetCore.Mvc;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Todos.Commands.CreateTodo;
using TodoPlatform.Application.Todos.Commands.DeleteTodo;
using TodoPlatform.Application.Todos.Commands.UpdateTodo;
using TodoPlatform.Application.Todos.Queries.GetTodoById;
using TodoPlatform.Application.Todos.Queries.GetTodos;

namespace TodoPlatform.Api.Controllers;

/// <summary>
/// Todo CRUD endpoints aligned with contracts/openapi.yaml.
/// </summary>
[ApiController]
[Route("api/todos")]
[Produces("application/json")]
public class TodosController(IMediator mediator) : ControllerBase
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
        var todos = await mediator.Send(new GetTodosQuery(userId), cancellationToken);
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
        var todo = await mediator.Send(new GetTodoByIdQuery(id), cancellationToken);
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
        var todo = await mediator.Send(
            new CreateTodoCommand(request.Title, request.UserId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = todo.Id }, todo);
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
        var todo = await mediator.Send(new UpdateTodoCommand(id, request), cancellationToken);
        return Ok(todo);
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
        await mediator.Send(new DeleteTodoCommand(id), cancellationToken);
        return NoContent();
    }
}
