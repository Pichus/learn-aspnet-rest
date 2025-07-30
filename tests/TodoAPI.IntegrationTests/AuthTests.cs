using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TodoAPI.Dtos;
using Xunit.Abstractions;

namespace TodoAPI.IntegrationTests;

[CollectionDefinition("AuthTests")]
public class SharedTestCollection : ICollectionFixture<CustomWebApplicationFactory>;

[Collection("AuthTests")]
public class AuthTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _testOutputHelper;

    public AuthTests(CustomWebApplicationFactory factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenUsernameItTaken()
    {
        var request = new UserDto
        {
            Username = "test_user",
            Password = "test_password"
        };

        await _factory.HttpClient.PostAsJsonAsync("api/Auth/register", request);

        var httpResponse = await _factory.HttpClient.PostAsJsonAsync("api/Auth/register", request);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenUsernameIsAvailable()
    {
        var request = new UserDto
        {
            Username = "available_username",
            Password = "test_password"
        };

        var httpResponse = await _factory.HttpClient.PostAsJsonAsync("api/Auth/register", request);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenCalledWithAWrongUsername()
    {
        var request = new UserDto
        {
            Username = "wrong_username",
            Password = "test_password"
        };

        var httpResponse = await _factory.HttpClient.PostAsJsonAsync("api/Auth/login", request);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenCalledWithAWrongPassword()
    {
        var request = new UserDto
        {
            Username = "test_user_does_not_exist",
            Password = "wrong_password"
        };

        var httpResponse = await _factory.HttpClient.PostAsJsonAsync("api/Auth/login", request);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCalledWithCorrectUsernameAndPassword()
    {
        var request = new UserDto
        {
            Username = "correct_username",
            Password = "correct_password"
        };

        await _factory.HttpClient.PostAsJsonAsync("api/Auth/register", request);

        var httpResponse = await _factory.HttpClient.PostAsJsonAsync("api/Auth/login", request);

        httpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}