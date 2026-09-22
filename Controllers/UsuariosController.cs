using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PotyInternosAPI.DTOs.Aplicacoes;
using PotyInternosAPI.DTOs.Usuarios;
using PotyInternosAPI.Services.Interfaces;

namespace PotyInternosAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _service;

    public UsuariosController(IUsuarioService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<UsuarioPageDto>> GetAll([FromQuery] UsuarioQueryDto query)
    {
        var result = await _service.GetAllAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarioResponseDto>> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioResponseDto>> Create([FromBody] UsuarioCreateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.UsuarioId }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UsuarioResponseDto>> Update(string id, [FromBody] UsuarioUpdateDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpPut("{id}/senha")]
    public async Task<IActionResult> ChangePassword(string id, [FromBody] UsuarioChangePasswordDto dto)
    {
        await _service.ChangePasswordAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{usuarioId}/aplicacoes")]
    public async Task<ActionResult<IEnumerable<AplicacaoResponseDto>>> GetAplicacoes(string usuarioId)
    {
        var result = await _service.GetAplicacoesAsync(usuarioId);
        return Ok(result);
    }

    [HttpPost("{usuarioId}/aplicacoes/{aplicacaoId}")]
    public async Task<IActionResult> VincularAplicacao(
        string usuarioId,
        string aplicacaoId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] VincularAplicacaoDto? dto = null)
    {
        await _service.VincularAplicacaoAsync(usuarioId, aplicacaoId, dto);
        return NoContent();
    }

    [HttpPut("{usuarioId}/aplicacoes/{aplicacaoId}/campos-adicionais")]
    public async Task<IActionResult> AtualizarCamposAplicacao(string usuarioId, string aplicacaoId, [FromBody] AtualizarCamposAplicacaoDto dto)
    {
        await _service.AtualizarCamposAplicacaoAsync(usuarioId, aplicacaoId, dto);
        return NoContent();
    }

    [HttpDelete("{usuarioId}/aplicacoes/{aplicacaoId}")]
    public async Task<IActionResult> DesvincularAplicacao(string usuarioId, string aplicacaoId)
    {
        await _service.DesvincularAplicacaoAsync(usuarioId, aplicacaoId);
        return NoContent();
    }
}
