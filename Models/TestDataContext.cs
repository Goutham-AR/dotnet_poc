using Microsoft.EntityFrameworkCore;

namespace streaming_dotnet.Models;

public class TestDataContext : DbContext
{
    public TestDataContext(DbContextOptions<TestDataContext> options)
        : base(options)
    {
    }

    public DbSet<TestData> Items { get; set; } = null!;
}
