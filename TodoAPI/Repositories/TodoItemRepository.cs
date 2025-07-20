using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoAPI.Repositories;

public class TodoItemRepository : ITodoItemRepository
{
    private readonly TodoContext _context;

    public TodoItemRepository(TodoContext context)
    {
        _context = context;
    }

    public async Task<ICollection<TodoItem>> GetAllAsync(int userId)
    {
        return await _context.TodoItems.Where(item => item.UserId == userId).ToListAsync();
    }

    public async Task<TodoItem?> GetByIdAsync(int id, int userId)
    {
        return await _context.TodoItems.FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);
    }

    public async Task<TodoItem?> GetByIdWithUserIncludedAsync(int id, int userId)
    {
        return await _context.TodoItems
            .Include(item => item.User)
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);
    }

    public async Task<bool> Remove(int id, int userId)
    {
        var result = true;
        var todoItem = await _context.TodoItems.FirstOrDefaultAsync(item => item.UserId == userId && item.Id == id);

        if (todoItem is null)
        {
            result = false;
            return result;
        }

        _context.TodoItems.Remove(todoItem);
        return result;
    }

    public void Add(TodoItem todoItem)
    {
        _context.TodoItems.Add(todoItem);
    }

    public async Task<bool> Update(TodoItem todoItem)
    {
        var result = true;
        var todoItemExists = await AnyAsync(item => item.UserId == todoItem.UserId && item.Id == todoItem.Id);

        if (!todoItemExists)
        {
            result = false;
            return result;
        }

        _context.TodoItems.Update(todoItem);

        return result;
    }

    public async Task<bool> AnyAsync(Expression<Func<TodoItem, bool>> predicate)
    {
        return await _context.TodoItems.AnyAsync(predicate);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}