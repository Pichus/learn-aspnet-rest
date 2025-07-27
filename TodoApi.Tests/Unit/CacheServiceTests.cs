using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TodoAPI.Services;

namespace TodoApi.Tests.Unit;

public class CacheServiceTests
{
    private readonly CacheService _cacheService;
    private readonly IDistributedCache _distributedCache;

    public CacheServiceTests()
    {
        _distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _cacheService = new CacheService(_distributedCache);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ReturnsDeserializedObject()
    {
        var key = "test-key";
        var testObject = new TestModel { Id = 1, Name = "Test" };
        var serializedJson = JsonSerializer.Serialize(testObject);
        var serializedBytes = Encoding.UTF8.GetBytes(serializedJson);

        await _distributedCache.SetAsync(key, serializedBytes);


        var result = await _cacheService.GetAsync<TestModel>(key);


        result.Should().NotBeNull();
        result.Id.Should().Be(testObject.Id);
        result.Name.Should().Be(testObject.Name);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        var key = "non-existent-key";


        var result = await _cacheService.GetAsync<TestModel>(key);


        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WhenCalled_StoresValueInDistributedCache()
    {
        var key = "test-key";
        var testObject = new TestModel { Id = 1, Name = "Test" };


        await _cacheService.SetAsync(key, testObject);

        var cachedBytes = await _distributedCache.GetAsync(key);
        cachedBytes.Should().NotBeNull();

        var cachedJson = Encoding.UTF8.GetString(cachedBytes!);
        var deserializedObject = JsonSerializer.Deserialize<TestModel>(cachedJson);

        deserializedObject.Should().NotBeNull();
        deserializedObject.Id.Should().Be(testObject.Id);
        deserializedObject.Name.Should().Be(testObject.Name);
    }

    [Fact]
    public async Task SetAsync_WhenCalledTwice_OverwritesValue()
    {
        var key = "test-key";
        var firstObject = new TestModel { Id = 1, Name = "First" };
        var secondObject = new TestModel { Id = 2, Name = "Second" };


        await _cacheService.SetAsync(key, firstObject);
        await _cacheService.SetAsync(key, secondObject);


        var result = await _cacheService.GetAsync<TestModel>(key);
        result.Should().NotBeNull();
        result.Id.Should().Be(secondObject.Id);
        result.Name.Should().Be(secondObject.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenValueExistsInCache_ReturnsFromCache()
    {
        var key = "test-key";
        var cachedObject = new TestModel { Id = 1, Name = "Cached" };
        var factoryObject = new TestModel { Id = 2, Name = "Factory" };

        await _cacheService.SetAsync(key, cachedObject);

        var factoryCalled = false;

        Task<TestModel?> Factory()
        {
            factoryCalled = true;
            return Task.FromResult<TestModel?>(factoryObject);
        }


        var result = await _cacheService.GetOrSetAsync(key, Factory);


        result.Should().NotBeNull();
        result.Id.Should().Be(cachedObject.Id);
        result.Name.Should().Be(cachedObject.Name);
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrSetAsync_WhenValueNotInCache_CallsFactoryAndCachesResult()
    {
        var key = "test-key";
        var factoryObject = new TestModel { Id = 1, Name = "Factory" };

        var factoryCalled = false;

        Task<TestModel?> Factory()
        {
            factoryCalled = true;
            return Task.FromResult<TestModel?>(factoryObject);
        }


        var result = await _cacheService.GetOrSetAsync(key, Factory);


        result.Should().NotBeNull();
        result.Id.Should().Be(factoryObject.Id);
        result.Name.Should().Be(factoryObject.Name);
        factoryCalled.Should().BeTrue();

        var cachedBytes = await _distributedCache.GetAsync(key);
        cachedBytes.Should().NotBeNull();

        var cachedJson = Encoding.UTF8.GetString(cachedBytes);
        var deserializedObject = JsonSerializer.Deserialize<TestModel>(cachedJson);

        deserializedObject.Should().NotBeNull();
        deserializedObject.Id.Should().Be(factoryObject.Id);
        deserializedObject.Name.Should().Be(factoryObject.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenFactoryReturnsNull_DoesNotCacheAndReturnsNull()
    {
        var key = "test-key";

        var factoryCalled = false;

        Task<TestModel?> Factory()
        {
            factoryCalled = true;
            return Task.FromResult<TestModel?>(null);
        }


        var result = await _cacheService.GetOrSetAsync(key, Factory);


        result.Should().BeNull();
        factoryCalled.Should().BeTrue();

        var cachedResult = await _cacheService.GetAsync<TestModel>(key);
        cachedResult.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WhenCalled_RemovesKeyFromCache()
    {
        var key = "test-key";
        var testObject = new TestModel { Id = 1, Name = "Test" };
        var serializedJson = JsonSerializer.Serialize(testObject);
        var serializedBytes = Encoding.UTF8.GetBytes(serializedJson);

        await _distributedCache.SetAsync(key, serializedBytes);

        var beforeRemove = await _distributedCache.GetAsync(key);
        beforeRemove.Should().NotBeNull();


        await _cacheService.RemoveAsync(key);

        var afterRemove = await _distributedCache.GetAsync(key);
        afterRemove.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WhenKeyDoesNotExist_DoesNotThrow()
    {
        var key = "non-existent-key";

        var act = async () => await _cacheService.RemoveAsync(key);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("")]
    [InlineData("simple-key")]
    [InlineData("key-with-special-chars-123!@#")]
    [InlineData("very-long-key-name-with-lots-of-characters-to-test-edge-cases")]
    public async Task CacheOperations_WithDifferentKeys_WorkCorrectly(string key)
    {
        var testObject = new TestModel { Id = 42, Name = "Test Object" };

        await _cacheService.SetAsync(key, testObject);
        var result = await _cacheService.GetAsync<TestModel>(key);

        result.Should().NotBeNull();
        result.Id.Should().Be(testObject.Id);
        result.Name.Should().Be(testObject.Name);

        await _cacheService.RemoveAsync(key);
        var afterRemove = await _cacheService.GetAsync<TestModel>(key);
        afterRemove.Should().BeNull();
    }

    [Fact]
    public async Task CacheService_WithComplexObject_SerializesAndDeserializesCorrectly()
    {
        var key = "complex-object";
        var complexObject = new ComplexTestModel
        {
            Id = 1,
            Name = "Complex Test",
            Items = new List<string> { "Item1", "Item2", "Item3" },
            Metadata = new Dictionary<string, object>
            {
                { "count", 42 },
                { "isActive", true },
                { "description", "Test metadata" }
            },
            CreatedAt = DateTime.UtcNow
        };


        await _cacheService.SetAsync(key, complexObject);
        var result = await _cacheService.GetAsync<ComplexTestModel>(key);

        result.Should().NotBeNull();
        result!.Id.Should().Be(complexObject.Id);
        result.Name.Should().Be(complexObject.Name);
        result.Items.Should().BeEquivalentTo(complexObject.Items);
        result.CreatedAt.Should().BeCloseTo(complexObject.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task GetAsync_WithDifferentTypes_WorksCorrectly()
    {
        await _cacheService.SetAsync("string-key", "test string");
        var stringResult = await _cacheService.GetAsync<string>("string-key");
        stringResult.Should().Be("test string");

        await _cacheService.SetAsync("different-key", new TestModel { Id = 999, Name = "Different" });
        var differentResult = await _cacheService.GetAsync<TestModel>("different-key");
        differentResult!.Id.Should().Be(999);
        differentResult.Name.Should().Be("Different");
    }

    public class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ComplexTestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}