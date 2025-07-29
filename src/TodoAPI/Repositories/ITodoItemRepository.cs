using System.Linq.Expressions;
using TodoApi.Models;

namespace TodoAPI.Repositories;

public interface ITodoItemRepository : IRepository
{
    Task<ICollection<TodoItem>> GetAllAsync(int userId);
    Task<TodoItem?> GetByIdAsync(int id, int userId);
    Task<TodoItem?> GetByIdWithUserIncludedAsync(int id, int userId);
    Task<bool> Remove(int itemId, int userId);
    void Add(TodoItem todoItem);
    Task<bool> Update(TodoItem todoItem);
    Task<bool> AnyAsync(Expression<Func<TodoItem, bool>> predicate);
    Task SaveChangesAsync();
}