namespace TodoAPI.Exceptions;

public class WrongUsernameOrPassword : Exception
{
    public WrongUsernameOrPassword()
    {
    }

    public WrongUsernameOrPassword(string message)
        : base(message)
    {
    }

    public WrongUsernameOrPassword(string message, Exception inner)
        : base(message, inner)
    {
    }
}