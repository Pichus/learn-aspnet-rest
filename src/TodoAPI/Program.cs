using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using TodoApi.Models;
using TodoAPI.Repositories;
using TodoAPI.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "TodoInstance";
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(jwtOptions =>
    {
        jwtOptions.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:Key"]!)),
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidAudience = config["JwtSettings:Audience"],
            ValidIssuer = config["JwtSettings:Issuer"],
            ClockSkew = TimeSpan.Zero // for testing purposes
        };
    });


builder.Services.AddControllers();
builder.Services.AddOpenApiDocument(document =>
{
    document.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
    {
        Type = OpenApiSecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        Description = "Type into the textbox: {your JWT token}."
    });

    document.OperationProcessors.Add(
        new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
});

builder.Services.AddDbContext<TodoContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<ITodoItemRepository>(provider =>
{
    var context = provider.GetService<TodoContext>();
    var cache = provider.GetService<ICacheService>();

    return new CacheTodoItemRepository(new TodoItemRepository(context), cache);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseHttpsRedirection();


app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program {}