using PotyInternosAPI.DTOs.Areas;

namespace PotyInternosAPI.Services.Interfaces;

public interface IAreaService
{
    Task<IEnumerable<AreaResponseDto>> GetAllAsync(bool includeInactive = false);
    Task<AreaResponseDto?> GetByIdAsync(string id);
    Task<AreaResponseDto> CreateAsync(AreaCreateDto dto);
    Task<AreaResponseDto> UpdateAsync(string id, AreaUpdateDto dto);
    Task DeleteAsync(string id);
}
