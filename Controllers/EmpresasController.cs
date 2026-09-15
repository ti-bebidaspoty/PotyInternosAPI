using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PotyInternosAPI.DTOs.Empresas;
using PotyInternosAPI.Services.Interfaces;

namespace PotyInternosAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/empresas")]
public class EmpresasController : ControllerBase
{
    private readonly IEmpresaService _service;

    public EmpresasController(IEmpresaService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmpresaResponseDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await _service.GetAllAsync(includeInactive);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmpresaResponseDto>> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<EmpresaResponseDto>> Create([FromBody] EmpresaCreateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.EmpresaId }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EmpresaResponseDto>> Update(string id, [FromBody] EmpresaUpdateDto dto)
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
