using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoAPI.Dtos;
using TodoApi.Models;
using TodoAPI.Repositories;

namespace TodoAPI.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TodoItemsController : ControllerBase
{
    private readonly ITodoItemRepository _todoItemRepository;

    public TodoItemsController(ITodoItemRepository todoItemRepository)
    {
        _todoItemRepository = todoItemRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetTodoItemDto>>> GetTodoItems()
    {
        var userId = GetUserIdFromJwt();

        var todoItems = await _todoItemRepository.GetAllAsync(userId);

        var todoItemDtos = todoItems.Select(todoItem => new GetTodoItemDto
        {
            Id = todoItem.Id,
            IsComplete = todoItem.IsComplete,
            Name = todoItem.Name,
            UserId = todoItem.UserId
        }).ToList();

        return Ok(todoItemDtos);
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<GetTodoItemDto>> GetTodoItem(int id)
    {
        var userId = GetUserIdFromJwt();

        var todoItem = await _todoItemRepository.GetByIdAsync(id, userId);
        
        if (todoItem == null) return NotFound();

        var todoItemDto = new GetTodoItemDto
        {
            Id = todoItem.Id,
            IsComplete = todoItem.IsComplete,
            Name = todoItem.Name,
            UserId = todoItem.UserId
        };

        return todoItemDto;
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> PutTodoItem([FromRoute] int id, [FromBody] CreateTodoItemDto todoItemDto)
    {
        var userId = GetUserIdFromJwt();

        var todoItem = new TodoItem
        {
            Id = id,
            Name = todoItemDto.Name,
            IsComplete = todoItemDto.IsComplete,
            UserId = userId,
        };

        bool result = await _todoItemRepository.Update(todoItem);

        if (!result)
        {
            return NotFound();
        }

        await _todoItemRepository.SaveChangesAsync();
        
        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<GetTodoItemDto>> PostTodoItem([FromBody] CreateTodoItemDto request)
    {
        var userId = GetUserIdFromJwt();

        var todoItem = new TodoItem
        {
            Name = request.Name,
            IsComplete = request.IsComplete,
            UserId = userId
        };

        _todoItemRepository.Add(todoItem);
        await _todoItemRepository.SaveChangesAsync();

        var response = new GetTodoItemDto
        {
            Id = todoItem.Id,
            Name = todoItem.Name,
            IsComplete = todoItem.IsComplete,
            UserId = todoItem.UserId
        };

        return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodoItem(int id)
    {
        var userId = GetUserIdFromJwt();

        bool result = await _todoItemRepository.Remove(id, userId);

        if (!result)
        {
            return NotFound();
        }

        await _todoItemRepository.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> TodoItemExists(int id)
    {
        return await _todoItemRepository.AnyAsync(e => e.Id == id);
    }

    private int GetUserIdFromJwt()
    {
        return int.Parse(User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)!.Value);
    }
}
