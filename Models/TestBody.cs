using System.ComponentModel.DataAnnotations;

namespace streaming_dotnet.Models;

public class TestBody
{
    [Required]
    [AllowedValues("csv", "xlsx", "pdf")]
    public required string Format { get; set; }
}
