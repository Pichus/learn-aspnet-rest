namespace TodoAPI.Dtos;

public class CreateTodoItemDto
{
    public string Name { get; set; }
    public bool IsComplete { get; set; }
}