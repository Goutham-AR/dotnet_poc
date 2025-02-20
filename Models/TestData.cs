using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace streaming_dotnet.Models;

public class TestData
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("data")]
    public string? Data { get; set; }
}
