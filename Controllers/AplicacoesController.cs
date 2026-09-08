using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PotyInternosAPI.DTOs.Aplicacoes;
using PotyInternosAPI.Services.Interfaces;

namespace PotyInternosAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/aplicacoes")]
public class AplicacoesController : ControllerBase
{
    private readonly IAplicacaoService _service;

    public AplicacoesController(IAplicacaoService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AplicacaoResponseDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await _service.GetAllAsync(includeInactive);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AplicacaoResponseDto>> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AplicacaoResponseDto>> Create([FromBody] AplicacaoCreateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.AplicacaoId }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AplicacaoResponseDto>> Update(string id, [FromBody] AplicacaoUpdateDto dto)
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
