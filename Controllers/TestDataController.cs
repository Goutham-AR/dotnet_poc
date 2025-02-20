using Microsoft.AspNetCore.Mvc;
using streaming_dotnet.Models;
using streaming_dotnet.Services;

[ApiController]
[Route("streaming")]
public class StreamingController : ControllerBase
{
    private TestDataService _service;

    public StreamingController(TestDataService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TestData>>> DownloadFile()
    {
        return await _service.GetAsync();
    }

}
