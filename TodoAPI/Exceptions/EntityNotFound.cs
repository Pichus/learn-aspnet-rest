namespace TodoAPI.Exceptions;

public class EntityNotFound : Exception
{
    public EntityNotFound()
    {
    }

    public EntityNotFound(string message)
        : base(message)
    {
    }

    public EntityNotFound(string message, Exception inner)
        : base(message, inner)
    {
    }
}