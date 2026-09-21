using Microsoft.AspNetCore.Mvc;
using RezervBooking.Application.Models;
using RezervBooking.Application.Services;

namespace RezervBooking.Api.Controllers;

[ApiController]
[Route("api/packages")]
public class PackagesController : ControllerBase
{
    private readonly PackageService _service;

    public PackagesController(PackageService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] long? packageId, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetPackagesAsync(packageId, cancellationToken));
    }

    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase(PurchasePackageRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.PurchaseAsync(request, cancellationToken);

        return Ok(result);
    }
}