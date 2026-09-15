using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PotyInternosAPI.DTOs.Areas;
using PotyInternosAPI.Services.Interfaces;

namespace PotyInternosAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/areas")]
public class AreasController : ControllerBase
{
    private readonly IAreaService _service;

    public AreasController(IAreaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AreaResponseDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await _service.GetAllAsync(includeInactive);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AreaResponseDto>> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AreaResponseDto>> Create([FromBody] AreaCreateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.AreaId }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AreaResponseDto>> Update(string id, [FromBody] AreaUpdateDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
