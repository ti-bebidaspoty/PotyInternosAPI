using PotyInternosAPI.DTOs.Departamentos;

namespace PotyInternosAPI.Services.Interfaces;

public interface IDepartamentoService
{
    Task<IEnumerable<DepartamentoResponseDto>> GetAllAsync(bool includeInactive = false);
    Task<DepartamentoResponseDto?> GetByIdAsync(string id);
    Task<DepartamentoResponseDto> CreateAsync(DepartamentoCreateDto dto);
    Task<DepartamentoResponseDto> UpdateAsync(string id, DepartamentoUpdateDto dto);
    Task DeleteAsync(string id);
}
