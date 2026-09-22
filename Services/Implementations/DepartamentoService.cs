using Microsoft.EntityFrameworkCore;
using PotyInternosAPI.Data;
using PotyInternosAPI.DTOs.Departamentos;
using PotyInternosAPI.Exceptions;
using PotyInternosAPI.Models;
using PotyInternosAPI.Services.Interfaces;

namespace PotyInternosAPI.Services.Implementations;

public class DepartamentoService : IDepartamentoService
{
    private readonly AppDbContext _context;

    public DepartamentoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DepartamentoResponseDto>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Departamentos
            .AsNoTracking()
            .Include(d => d.Area)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(d => d.Status);
        }

        return await query
            .Select(d => new DepartamentoResponseDto
            {
                DepartamentoId = d.DepartamentoId,
                Departamento = d.Nome,
                AreaId = d.AreaId,
                Area = d.Area.Nome,
                Status = d.Status,
                CodigoAlternativo = d.CodigoAlternativo
            })
            .ToListAsync();
    }

    public async Task<DepartamentoResponseDto?> GetByIdAsync(string id)
    {
        return await _context.Departamentos
            .AsNoTracking()
            .Where(d => d.DepartamentoId == id)
            .Select(d => new DepartamentoResponseDto
            {
                DepartamentoId = d.DepartamentoId,
                Departamento = d.Nome,
                AreaId = d.AreaId,
                Area = d.Area.Nome,
                Status = d.Status,
                CodigoAlternativo = d.CodigoAlternativo
            })
            .FirstOrDefaultAsync();
    }

    public async Task<DepartamentoResponseDto> CreateAsync(DepartamentoCreateDto dto)
    {
        var area = await _context.Areas.FirstOrDefaultAsync(a => a.AreaId == dto.AreaId)
            ?? throw new ValidationException($"Área '{dto.AreaId}' não existe.");

        if (!area.Status)
        {
            throw new ValidationException("A área informada está inativa.");
        }

        var nomeEmUso = await _context.Departamentos
            .AnyAsync(d => d.Nome == dto.Departamento);

        if (nomeEmUso)
        {
            throw new ConflictException($"Já existe um departamento com o nome '{dto.Departamento}'.");
        }

        var entity = new Departamento
        {
            DepartamentoId = Guid.NewGuid().ToString(),
            Nome = dto.Departamento,
            AreaId = dto.AreaId,
            Status = true
        };

        await _context.Departamentos.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new DepartamentoResponseDto
        {
            DepartamentoId = entity.DepartamentoId,
            Departamento = entity.Nome,
            AreaId = entity.AreaId,
            Area = area.Nome,
            Status = entity.Status
        };
    }

    public async Task<DepartamentoResponseDto> UpdateAsync(string id, DepartamentoUpdateDto dto)
    {
        var entity = await _context.Departamentos.FirstOrDefaultAsync(d => d.DepartamentoId == id)
            ?? throw new NotFoundException($"Departamento '{id}' não encontrado.");

        var area = await _context.Areas.FirstOrDefaultAsync(a => a.AreaId == dto.AreaId)
            ?? throw new ValidationException($"Área '{dto.AreaId}' não existe.");

        if (!area.Status)
        {
            throw new ValidationException("A área informada está inativa.");
        }

        var nomeEmUso = await _context.Departamentos
            .AnyAsync(d => d.Nome == dto.Departamento && d.DepartamentoId != id);

        if (nomeEmUso)
        {
            throw new ConflictException($"Já existe um departamento com o nome '{dto.Departamento}'.");
        }

        entity.Nome = dto.Departamento;
        entity.AreaId = dto.AreaId;
        entity.Status = dto.Status;

        await _context.SaveChangesAsync();

        return new DepartamentoResponseDto
        {
            DepartamentoId = entity.DepartamentoId,
            Departamento = entity.Nome,
            AreaId = entity.AreaId,
            Area = area.Nome,
            Status = entity.Status
        };
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.Departamentos.FirstOrDefaultAsync(d => d.DepartamentoId == id)
            ?? throw new NotFoundException($"Departamento '{id}' não encontrado.");

        entity.Status = false;
        await _context.SaveChangesAsync();
    }
}
