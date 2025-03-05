using streaming_dotnet.Models;
using streaming_dotnet.Hubs;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;

using MongoDB.Driver;
using MongoDB.Bson;
using CsvHelper;
using OfficeOpenXml;

namespace streaming_dotnet.Services;

public class TestDataService
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly IMongoCollection<BsonDocument> _collection2;
    private readonly ILogger<TestDataService> _logger;
    private readonly IHubContext<TestHub> _hub;
    private readonly UserService _userService;
    private readonly int _batchSize = 1000;
    private readonly string _hostName = "https://192.168.10.83:7263";

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

    public async Task StreamData1(string id, string format)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var filename = $"output1.{format}";
            await Stream(_collection, id, format, filename);
        }
        catch (Exception e)
        {
            _logger.LogError($"error: {e.Message}");
            await Task.Delay(2000);
            await _hub.Clients.All.SendAsync("Error", new { Message = e.Message, Id = id });
        }
        watch.Stop();
        await _hub.Clients.All.SendAsync("Time", new { TimeTaken = watch.ElapsedMilliseconds });
    }

    public async Task StreamData2(string id, string format)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var filename = $"output2.{format}";
            await Stream(_collection2, id, format, filename);
        }
        catch (Exception e)
        {
            _logger.LogError($"error: {e.Message}");
            await Task.Delay(2000);
            await _hub.Clients.All.SendAsync("Error", new { Message = e.Message, Id = id });
        }
        watch.Stop();
        await _hub.Clients.All.SendAsync("Time", new { TimeTaken = watch.ElapsedMilliseconds });
    }

    private async Task Stream(IMongoCollection<BsonDocument> collection, string id, string format, string filename)
    {
        if (format == "csv")
        {
            /*await StreamUsingCursor(collection, filename, id);*/
            await StreamCsv(collection, filename, id);
        }
        else if (format == "xlsx")
        {
            await StreamExcel(collection, filename, id);
        }
    }

    private async Task StreamExcel(IMongoCollection<BsonDocument> collection, string filename, string id)
    {
        int skip = 0;
        LogMemoryUsage("streaming initial");
        var count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        using (var stream = new FileStream($"download/{filename}", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
        using (var package = new ExcelPackage(stream))
        {
            var headerWritten = false;
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");
            while (true)
            {
                // Get total count as well to calculate the progress percentage (not optimal, use facet aggregation instead)
                var batch = await collection.Find(FilterDefinition<BsonDocument>.Empty).Skip(skip).Limit(_batchSize).ToListAsync();
                LogMemoryUsage("streaming middle");
                if (batch.Count == 0)
                {
                    break;
                }

                var rowNum = skip + 2;
                foreach (var doc in batch)
                {
                    var dictionary = doc.ToDictionary();
                    if (!headerWritten)
                    {
                        var keys = dictionary.Keys;
                        for (int i = 0; i < keys.Count; ++i)
                        {
                            worksheet.Cells[1, i + 1].Value = keys.ElementAt(i)!;
                        }
                        headerWritten = true;
                    }

                    var values = dictionary.Values;
                    for (int j = 0; j < values.Count; ++j)
                    {
                        worksheet.Cells[rowNum, j + 1].Value = values.ElementAt(j)!;
                    }
                    ++rowNum;
                }
                await package.SaveAsync();
                skip += _batchSize;
                var progress = (100.0 * skip) / count;
                progress = progress > 100 ? 100 : Math.Round(progress, 1);
                _logger.LogInformation($"sending progress info: {progress}");
                await _hub.Clients.All.SendAsync("Progress", new { Progress = progress, Id = id });
            }
        }
        var fileUrl = $"{_hostName}/download/{filename}";
        _logger.LogInformation($"sending complete event");
        await _hub.Clients.All.SendAsync("Complete", new { FileUrl = fileUrl, Id = id });
    }

    private async Task StreamCsv(IMongoCollection<BsonDocument> collection, string filename, string id)
    {

        bool headerWritten = false;
        int batchSize = 1000;
        int skip = 0;

        var writer = new StreamWriter($"download/{filename}");
        var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
        var count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        while (true)
        {
            // Get total count as well to calculate the progress percentage (not optimal, use facet aggregation instead)
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

    private async Task StreamUsingCursor(IMongoCollection<BsonDocument> collection, string filename, string id)
    {
        LogMemoryUsage("initial memory usage");
        var docsCount = 0;
        var headerWritten = false;
        var writer = new StreamWriter($"download/{filename}");
        var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
        var count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        var findOptions = new FindOptions<BsonDocument> { BatchSize = _batchSize };
        using (var cursor = await collection.FindAsync(_ => true, findOptions))
        {
            LogMemoryUsage("after find memory usage");
            while (await cursor.MoveNextAsync())
            {
                LogMemoryUsage("each iteration memory usage");
                var docs = cursor.Current;
                foreach (var doc in docs)
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
                    ++docsCount;
                }
                var progress = (100.0 * docsCount) / count;
                progress = progress > 100 ? 100 : Math.Round(progress, 1);
                _logger.LogInformation($"sending progress info: {progress}");
                await _hub.Clients.All.SendAsync("Progress", new { Progress = progress, Id = id });
            }
        }
        await writer.FlushAsync();
        var fileUrl = $"{_hostName}/download/{filename}";
        _logger.LogInformation($"sending complete event");
        await _hub.Clients.All.SendAsync("Complete", new { FileUrl = fileUrl, Id = id });

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

