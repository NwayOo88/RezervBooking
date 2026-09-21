using Microsoft.AspNetCore.Mvc;
using RezervBooking.Application.Models;
using RezervBooking.Application.Services;

[ApiController]
[Route("api/waitlist")]
public class WaitlistController : ControllerBase
{
    private readonly WaitlistService _service;

    public WaitlistController(WaitlistService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Join(JoinWaitlistRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _service.JoinAsync(request, cancellationToken));
    }
}