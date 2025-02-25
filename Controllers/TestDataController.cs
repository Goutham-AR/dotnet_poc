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
        _service.StreamData1();
        return Ok(new { Message = "Processing started"});
    }

    [HttpGet("streaming2")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadFile2()
    {
        _service.StreamData2();
        return Ok(new { Message = "Processing started"});
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> FullData()
    {
        var fileUrl = await _service.GetData();
        return Ok(new { FileUrl = fileUrl });
    }
}
