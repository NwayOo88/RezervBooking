using Microsoft.AspNetCore.Mvc;
using RezervBooking.Application.Models;
using RezervBooking.Application.Services;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly BookingService _service;

    public BookingsController(BookingService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Book(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.BookAsync(request, cancellationToken);

        return Ok(result);
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(CancelBookingRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CancelAsync(request, cancellationToken);

        return Ok(result);
    }
}