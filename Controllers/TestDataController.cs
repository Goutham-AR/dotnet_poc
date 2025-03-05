using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using streaming_dotnet.Services;
using streaming_dotnet.Hubs;
using streaming_dotnet.Models;
using Hangfire;

[ApiController]
[Route("data")]
public class StreamingController : ControllerBase
{
    private TestDataService _service;
    private IHubContext<TestHub> _hub;
    private readonly string[] _supportedFormats = { "csv", "excel", "pdf" };

    public StreamingController(TestDataService service, IHubContext<TestHub> hub)
    {
        _service = service;
        _hub = hub;
    }

    [HttpPost("streaming1")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult DownloadFile1([FromBody] TestBody body)
    {
        Guid guid = Guid.NewGuid();
        string id = guid.ToString();
        /*_service.StreamData1(id, body.Format);*/
        BackgroundJob.Enqueue(() => _service.StreamData1(id, body.Format));
        return Ok(new { Message = "Processing started", Id = id });
    }

    [HttpPost("streaming2")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult DownloadFile2([FromBody] TestBody body)
    {
        Guid guid = Guid.NewGuid();
        string id = guid.ToString();
        _service.StreamData2(id, body.Format);
        BackgroundJob.Enqueue(() => _service.StreamData2(id, body.Format));
        return Ok(new { Message = "Processing started", Id = id });
    }
}
