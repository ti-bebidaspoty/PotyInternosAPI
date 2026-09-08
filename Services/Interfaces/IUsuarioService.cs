using PotyInternosAPI.DTOs.Aplicacoes;
using PotyInternosAPI.DTOs.Usuarios;

namespace PotyInternosAPI.Services.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(bool includeInactive = false);
    Task<UsuarioResponseDto?> GetByIdAsync(string id);
    Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto dto);
    Task<UsuarioResponseDto> UpdateAsync(string id, UsuarioUpdateDto dto);
    Task ChangePasswordAsync(string id, UsuarioChangePasswordDto dto);
    Task DeleteAsync(string id);

    Task<IEnumerable<AplicacaoResponseDto>> GetAplicacoesAsync(string usuarioId);
    Task VincularAplicacaoAsync(string usuarioId, string aplicacaoId);
    Task DesvincularAplicacaoAsync(string usuarioId, string aplicacaoId);
}
