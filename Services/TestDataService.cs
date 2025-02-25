using streaming_dotnet.Models;
using streaming_dotnet.Hubs;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;

using MongoDB.Driver;
using MongoDB.Bson;
using CsvHelper;

namespace streaming_dotnet.Services;

public class TestDataService
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly IMongoCollection<BsonDocument> _collection2;
    private readonly ILogger<TestDataService> _logger;
    private readonly IHubContext<TestHub> _hub;
    private readonly UserService _userService;
    private readonly string _hostName = "http://192.168.10.83:5156";

    public TestDataService(IOptions<TestDatabaseSettings> testDatabaseSettings, ILogger<TestDataService> logger, IHubContext<TestHub> hub, UserService service)
    {
        _userService = service;
        _logger = logger;
        _hub = hub;
        var mongoClient = new MongoClient(testDatabaseSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(testDatabaseSettings.Value.DatabaseName);
        _collection = mongoDatabase.GetCollection<BsonDocument>(testDatabaseSettings.Value.CollectionName);
        var mongoDatabase2 = mongoClient.GetDatabase("TestDatabase");
        _collection2 = mongoDatabase2.GetCollection<BsonDocument>("TestCollection");
    }

    public async Task<string> GetData()
    {
        LogMemoryUsage("normal initial");
        var data = await _collection.FindAsync(FilterDefinition<BsonDocument>.Empty);
        var list = await data.ToListAsync();
        LogMemoryUsage("normal middle");

        var writer = new StreamWriter("downloads/normal_output.csv");
        var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
        bool headerWritten = false;
        WriteToCsv(csv, list, headerWritten);
        return $"{_hostName}/download/normal_output.csv";
    }

    public async Task StreamData1(string id)
    {
        await Stream(_collection, "output1.csv", id);
    }

    public async Task StreamData2(string id)
    {
        await Stream(_collection2, "output2.csv", id);
    }

    private async Task Stream(IMongoCollection<BsonDocument> collection, string filename, string id)
    {
        try
        {

            LogMemoryUsage("streaming initial");
            bool headerWritten = false;
            int batchSize = 1000;
            int skip = 0;

            var writer = new StreamWriter($"downloads/{filename}");
            var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
            var count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            while (true)
            {
                // Get total count as well to calculate the progress percentage (use facet aggregation)
                var batch = await collection.Find(FilterDefinition<BsonDocument>.Empty).Skip(skip).Limit(batchSize).ToListAsync();
                LogMemoryUsage("streaming middle");
                if (batch.Count == 0)
                {
                    break;
                }
                WriteToCsv(csv, batch, headerWritten);
                headerWritten = true;
                await writer.FlushAsync();
                skip += batchSize;

                var progress = (100.0 * skip) / count;
                progress = progress > 100 ? 100 : Math.Round(progress, 1);
                _logger.LogInformation($"sending progress info: {progress}");
                await _hub.Clients.All.SendAsync("Progress", new { Progress = progress, Id = id });
            }
            var fileUrl = $"{_hostName}/download/{filename}";
            _logger.LogInformation($"sending complete event");
            await _hub.Clients.All.SendAsync("Complete", new { FileUrl = fileUrl, Id = id });
        }
        catch (Exception e)
        {
            _logger.LogError($"error: {e.Message}");
            await _hub.Clients.All.SendAsync("Error", new { Message = e.Message, Id = id });
        }
    }

    private void WriteToCsv(CsvWriter csv, List<BsonDocument> data, bool headerWritten)
    {
        foreach (var doc in data)
        {
            var dictionary = doc.ToDictionary();
            if (!headerWritten)
            {
                foreach (var key in dictionary.Keys)
                {
                    csv.WriteField(key);
                }
                csv.NextRecord();
                headerWritten = true;
            }

            foreach (var value in dictionary.Values)
            {
                csv.WriteField(value?.ToString());
            }
            csv.NextRecord();
        }

    }

    private void LogMemoryUsage(string label)
    {
        long memoryUsed = GC.GetTotalMemory(true);
        double inMb = memoryUsed / 1000000.0;
        _logger.LogInformation($"{label}, memory usage: {inMb}");
    }

}
