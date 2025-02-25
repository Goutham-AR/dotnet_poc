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

    [HttpGet("streaming")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadFile([FromQuery] string id)
    {
        if (id == null)
        {
            return StatusCode(StatusCodes.Status400BadRequest);
        }
        _service.StreamData(id);
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
