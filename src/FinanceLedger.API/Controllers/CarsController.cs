using Asp.Versioning;
using FinanceLedger.Application.DTOs;
using FinanceLedger.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cars")]
[Authorize]
[Produces("application/json")]
public class CarsController : ControllerBase
{
    private readonly ICarService _carService;

    public CarsController(ICarService carService)
    {
        _carService = carService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CarDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CarDto>>> GetAll(CancellationToken ct)
        => Ok(await _carService.GetAllAsync(ct));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarDto>> GetById(string id, CancellationToken ct)
    {
        var car = await _carService.GetByIdAsync(id, ct);
        return car is null ? NotFound() : Ok(car);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CarDto>> Create([FromBody] CreateCarDto dto, CancellationToken ct)
    {
        var created = await _carService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { version = "1.0", id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CarDto>> Update(string id, [FromBody] UpdateCarDto dto, CancellationToken ct)
        => Ok(await _carService.UpdateAsync(id, dto, ct));
}
