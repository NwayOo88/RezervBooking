using Microsoft.AspNetCore.Mvc;
using RezervBooking.Application.Services;

[ApiController]
[Route("api/timetable")]
public class TimetableController : ControllerBase
{
    private readonly TimetableService _service;

    public TimetableController(TimetableService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] long? businessId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetAsync(businessId, date, cancellationToken));
    }
}