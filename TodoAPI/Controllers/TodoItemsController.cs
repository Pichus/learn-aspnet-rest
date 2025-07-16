using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoAPI.Dtos;
using TodoApi.Models;

namespace TodoAPI.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TodoItemsController : ControllerBase
{
    private readonly TodoContext _context;

    public TodoItemsController(TodoContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetTodoItemDto>>> GetTodoItems()
    {
        var userId = GetUserIdFromJwt();

        var todoItems = await _context.TodoItems.Where(item => item.UserId == userId).ToListAsync();

        var todoItemDtos = new List<GetTodoItemDto>();

        foreach (var todoItem in todoItems)
            todoItemDtos.Add(new GetTodoItemDto
            {
                Id = todoItem.Id,
                IsComplete = todoItem.IsComplete,
                Name = todoItem.Name,
                UserId = todoItem.UserId
            });

        return Ok(todoItemDtos);
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<GetTodoItemDto>> GetTodoItem(int id)
    {
        var userId = GetUserIdFromJwt();

        var todoItem = await _context.TodoItems.FirstOrDefaultAsync(item => item.UserId == userId && item.Id == id);

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
        
        var todoItem = await _context.TodoItems.FirstOrDefaultAsync(item => item.UserId == userId && item.Id == id);

        if (todoItem is null) return NotFound();
        
        todoItem.Name = todoItemDto.Name;
        todoItem.IsComplete = todoItemDto.IsComplete;

        await _context.SaveChangesAsync();

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

        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();

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
        var todoItem = await _context.TodoItems.FirstOrDefaultAsync(item => item.UserId == userId && item.Id == id);
        
        if (todoItem == null) return NotFound();
        
        _context.TodoItems.Remove(todoItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TodoItemExists(int id)
    {
        return _context.TodoItems.Any(e => e.Id == id);
    }

    private int GetUserIdFromJwt()
    {
        return int.Parse(User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)!.Value);
    }
}

// eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiaWxsaWEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjUiLCJleHAiOjE3NTI0MTg5MTQsImlzcyI6InBpY2h1c1RoZUlzc3VlciIsImF1ZCI6InBpY2h1c1RoZUF1ZGllbmNlIn0.JH3QKLbXKKKckbwAPzgB-9Ntl7XXe_0dB2ECrHB_Z7IKQmQkj7sTwjEWndZikJPF3lzp1wLtvBWUAwGHjePzVA