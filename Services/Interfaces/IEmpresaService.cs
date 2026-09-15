using PotyInternosAPI.DTOs.Empresas;

namespace PotyInternosAPI.Services.Interfaces;

public interface IEmpresaService
{
    Task<IEnumerable<EmpresaResponseDto>> GetAllAsync(bool includeInactive = false);
    Task<EmpresaResponseDto?> GetByIdAsync(string id);
    Task<EmpresaResponseDto> CreateAsync(EmpresaCreateDto dto);
    Task<EmpresaResponseDto> UpdateAsync(string id, EmpresaUpdateDto dto);
    Task DeleteAsync(string id);
}
