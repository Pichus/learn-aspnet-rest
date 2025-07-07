namespace TodoAPI.Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;

    public LoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Console.WriteLine("----------------------------");

        Console.WriteLine("request method");
        Console.WriteLine(context.Request.Method);

        Console.WriteLine("request headers");
        Console.WriteLine(context.Request.Headers.ToString());

        Console.WriteLine("request body:");
        Console.WriteLine(context.Request.Body.ToString());

        Console.WriteLine("----------------------------");
        
        
        await _next(context);
    }
}

public static class LoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseMyLogging(this IApplicationBuilder applicationBuilder)
    {
        return applicationBuilder.UseMiddleware<LoggingMiddleware>();
    }
}