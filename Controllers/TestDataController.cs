using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using streaming_dotnet.Services;
using streaming_dotnet.Hubs;

[ApiController]
[Route("data")]
public class StreamingController : ControllerBase
{
    private TestDataService _service;
    private IHubContext<TestHub> _hub;

   public StreamingController(TestDataService service, IHubContext<TestHub> hub)
    {
        _service = service;
        _hub = hub;
    }

    [HttpGet("streaming1")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadFile1()
    {
        Guid guid = Guid.NewGuid();
        string id = guid.ToString();
        _service.StreamData1(id);
        return Ok(new { Message = "Processing started", Id = id });
    }

    [HttpGet("streaming2")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadFile2()
    {
        Guid guid = Guid.NewGuid();
        string id = guid.ToString();
        _service.StreamData2(id);
        return Ok(new { Message = "Processing started", Id = id });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> FullData()
    {
        var fileUrl = await _service.GetData();
        return Ok(new { FileUrl = fileUrl });
    }
}
