using PotyInternosAPI.DTOs.Aplicacoes;

namespace PotyInternosAPI.Services.Interfaces;

public interface IAplicacaoService
{
    Task<IEnumerable<AplicacaoResponseDto>> GetAllAsync(bool includeInactive = false);
    Task<AplicacaoResponseDto?> GetByIdAsync(string id);
    Task<AplicacaoResponseDto> CreateAsync(AplicacaoCreateDto dto);
    Task<AplicacaoResponseDto> UpdateAsync(string id, AplicacaoUpdateDto dto);
    Task DeleteAsync(string id);
}
