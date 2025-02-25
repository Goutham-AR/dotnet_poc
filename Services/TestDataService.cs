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
    private readonly ILogger<TestDataService> _logger;
    private readonly IHubContext<TestHub> _hub;
    private readonly UserService _userService;

    public TestDataService(IOptions<TestDatabaseSettings> testDatabaseSettings, ILogger<TestDataService> logger, IHubContext<TestHub> hub, UserService service)
    {
        _userService = service;
        _logger = logger;
        _hub = hub;
        var mongoClient = new MongoClient(testDatabaseSettings.Value.ConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(testDatabaseSettings.Value.DatabaseName);
        _collection = mongoDatabase.GetCollection<BsonDocument>(testDatabaseSettings.Value.CollectionName);
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
        return "http://192.168.10.83:5156/download/normal_output.csv";
    }

    public async Task StreamData(string id)
    {
        var connectionId = _userService.GetConnectionId(id);
        LogMemoryUsage("streaming initial");
        bool headerWritten = false;
        int batchSize = 500;
        int skip = 0;

        var writer = new StreamWriter("downloads/output.csv");
        var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

        var count = await _collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        var estimatedTotalSizeMB = 0.0;
        while (true)
        {
            // Get total count as well to calculate the progress percentage (use facet aggregation)
            var batch = await _collection.Find(FilterDefinition<BsonDocument>.Empty).Skip(skip).Limit(batchSize).ToListAsync();
            LogMemoryUsage("streaming middle");
            if (batch.Count == 0)
            {
                break;
            }
            if (skip == 0)
            {
                long sampleSizeInBytes = batch.Sum(doc => doc.ToBson().Length);
                double sampleSizePerDoc = batch.Count > 0 ? (double)sampleSizeInBytes / batch.Count : 0;
                estimatedTotalSizeMB = (sampleSizePerDoc * count) / (1024.0 * 1024.0);
                _logger.LogInformation($"{estimatedTotalSizeMB}");
                break;
            }
            WriteToCsv(csv, batch, headerWritten);
            headerWritten = true;
            await writer.FlushAsync();
            skip += batchSize;

            var progress = (100.0 * skip) / count;
            progress = progress > 100 ? 100 : Math.Round(progress, 1);
            if (connectionId != null)
            {
                _logger.LogInformation($"sending progress info: {progress}");
                /*await _hub.Clients.Client(connectionId).SendAsync("Progress", new { Progress = progress });*/
                await _hub.Clients.All.SendAsync("Progress", new { Progress = progress });
            }
        }
        if (connectionId != null)
        {
            var fileUrl = "http://192.168.10.83:5156/download/output.csv";
            _logger.LogInformation($"sending complete event");
            await _hub.Clients.All.SendAsync("Complete", new { FileUrl = fileUrl});
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
