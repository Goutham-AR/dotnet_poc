public class LocalFileWriter : IFileWriter
{
    private readonly string _filepath;

    public LocalFileWriter(string filepath)
    {
        _filepath = filepath;
    }

    public async Task WriteAsync(Stream stream)
    {
        using var fileStream = new FileStream(_filepath, FileMode.Create, FileAccess.ReadWrite);
        await stream.CopyToAsync(fileStream);
    }
}
