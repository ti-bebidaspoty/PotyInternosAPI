using Microsoft.EntityFrameworkCore;
using PotyInternosAPI.Data;
using PotyInternosAPI.DTOs.Areas;
using PotyInternosAPI.Exceptions;
using PotyInternosAPI.Models;
using PotyInternosAPI.Services.Interfaces;

namespace PotyInternosAPI.Services.Implementations;

public class AreaService : IAreaService
{
    private readonly AppDbContext _context;

    public AreaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AreaResponseDto>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Areas.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(a => a.Status);
        }

        return await query
            .Select(a => new AreaResponseDto
            {
                AreaId = a.AreaId,
                Area = a.Nome,
                Status = a.Status
            })
            .ToListAsync();
    }

    public async Task<AreaResponseDto?> GetByIdAsync(string id)
    {
        return await _context.Areas
            .AsNoTracking()
            .Where(a => a.AreaId == id)
            .Select(a => new AreaResponseDto
            {
                AreaId = a.AreaId,
                Area = a.Nome,
                Status = a.Status
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AreaResponseDto> CreateAsync(AreaCreateDto dto)
    {
        var nomeEmUso = await _context.Areas
            .AnyAsync(a => a.Nome == dto.Area);

        if (nomeEmUso)
        {
            throw new ConflictException($"Já existe uma área com o nome '{dto.Area}'.");
        }

        var entity = new Area
        {
            AreaId = Guid.NewGuid().ToString(),
            Nome = dto.Area,
            Status = true
        };

        await _context.Areas.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new AreaResponseDto
        {
            AreaId = entity.AreaId,
            Area = entity.Nome,
            Status = entity.Status
        };
    }

    public async Task<AreaResponseDto> UpdateAsync(string id, AreaUpdateDto dto)
    {
        var entity = await _context.Areas.FirstOrDefaultAsync(a => a.AreaId == id)
            ?? throw new NotFoundException($"Área '{id}' não encontrada.");

        var nomeEmUso = await _context.Areas
            .AnyAsync(a => a.Nome == dto.Area && a.AreaId != id);

        if (nomeEmUso)
        {
            throw new ConflictException($"Já existe uma área com o nome '{dto.Area}'.");
        }

        entity.Nome = dto.Area;
        entity.Status = dto.Status;

        await _context.SaveChangesAsync();

        return new AreaResponseDto
        {
            AreaId = entity.AreaId,
            Area = entity.Nome,
            Status = entity.Status
        };
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.Areas.FirstOrDefaultAsync(a => a.AreaId == id)
            ?? throw new NotFoundException($"Área '{id}' não encontrada.");

        var possuiDepartamentos = await _context.Departamentos
            .AnyAsync(d => d.AreaId == id && d.Status);

        if (possuiDepartamentos)
        {
            throw new ConflictException("Não é possível inativar a área porque existem departamentos ativos vinculados a ela.");
        }

        entity.Status = false;
        await _context.SaveChangesAsync();
    }
}
