using streaming_dotnet.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace streaming_dotnet.Services;

public class TestDataService
{
    private readonly IMongoCollection<TestData> _testDataCollection;

    public TestDataService(IOptions<TestDatabaseSettings> testDatabaseSettings)
    {
        var mongoClient = new MongoClient(testDatabaseSettings.Value.ConnectionString);
        System.Console.WriteLine(testDatabaseSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(testDatabaseSettings.Value.DatabaseName);
        _testDataCollection = mongoDatabase.GetCollection<TestData>(testDatabaseSettings.Value.CollectionName);
    }

    public async Task<List<TestData>> GetAsync() => await _testDataCollection.Find(_ => true).ToListAsync();
}
