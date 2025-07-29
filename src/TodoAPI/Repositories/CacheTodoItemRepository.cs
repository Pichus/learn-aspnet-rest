using System.Linq.Expressions;
using TodoApi.Models;
using TodoAPI.Services;

namespace TodoAPI.Repositories;

public class CacheTodoItemRepository : ITodoItemRepository
{
    private readonly ICacheService _cacheService;
    private readonly ITodoItemRepository _decorated;

    public CacheTodoItemRepository(ITodoItemRepository decorated, ICacheService cacheService)
    {
        _decorated = decorated;
        _cacheService = cacheService;
    }

    public async Task<ICollection<TodoItem>> GetAllAsync(int userId)
    {
        return await _cacheService.GetOrSetAsync<ICollection<TodoItem>>($"todoItems_by_user-{userId}", async () =>
        {
            return await _decorated.GetAllAsync(userId);
        }) ?? [];
    }

    public async Task<TodoItem?> GetByIdAsync(int id, int userId)
    {
        return await _cacheService.GetOrSetAsync<TodoItem>($"todoItems-{id}", async () =>
        {
            return await _decorated.GetByIdAsync(id, userId);
        });
    }

    public async Task<TodoItem?> GetByIdWithUserIncludedAsync(int id, int userId)
    {
        return await _cacheService.GetOrSetAsync<TodoItem>($"todoItems_user_included-{id}", async () =>
        {
            return await _decorated.GetByIdWithUserIncludedAsync(id, userId);
        });
    }

    public async Task<bool> Remove(int itemId, int userId)
    {
        return await _decorated.Remove(itemId, userId);
    }

    public void Add(TodoItem todoItem)
    {
        _decorated.Add(todoItem);
    }

    public async Task<bool> Update(TodoItem todoItem)
    {
        return await _decorated.Update(todoItem);
    }

    public async Task<bool> AnyAsync(Expression<Func<TodoItem, bool>> predicate)
    {
        return await _decorated.AnyAsync(predicate);
    }

    public async Task SaveChangesAsync()
    {
        await _decorated.SaveChangesAsync();
    }
}