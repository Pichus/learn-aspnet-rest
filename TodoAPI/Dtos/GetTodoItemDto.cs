namespace TodoAPI.Dtos;

public class GetTodoItemDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsComplete { get; set; }
    public int UserId { get; set; }
}