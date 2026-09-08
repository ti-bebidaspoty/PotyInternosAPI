using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PotyInternosAPI.DTOs.Departamentos;
using PotyInternosAPI.Services.Interfaces;

namespace PotyInternosAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/departamentos")]
public class DepartamentosController : ControllerBase
{
    private readonly IDepartamentoService _service;

    public DepartamentosController(IDepartamentoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartamentoResponseDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await _service.GetAllAsync(includeInactive);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DepartamentoResponseDto>> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<DepartamentoResponseDto>> Create([FromBody] DepartamentoCreateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.DepartamentoId }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DepartamentoResponseDto>> Update(string id, [FromBody] DepartamentoUpdateDto dto)
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
