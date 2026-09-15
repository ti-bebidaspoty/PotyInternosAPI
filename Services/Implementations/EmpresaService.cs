using Microsoft.EntityFrameworkCore;
using PotyInternosAPI.Data;
using PotyInternosAPI.DTOs.Empresas;
using PotyInternosAPI.Exceptions;
using PotyInternosAPI.Models;
using PotyInternosAPI.Services.Interfaces;

namespace PotyInternosAPI.Services.Implementations;

public class EmpresaService : IEmpresaService
{
    private readonly AppDbContext _context;

    public EmpresaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EmpresaResponseDto>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Empresas.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(e => e.Status);
        }

        return await query
            .Select(e => new EmpresaResponseDto
            {
                EmpresaId = e.EmpresaId,
                Empresa = e.Nome,
                CodigoAlternativo = e.CodigoAlternativo,
                Status = e.Status
            })
            .ToListAsync();
    }

    public async Task<EmpresaResponseDto?> GetByIdAsync(string id)
    {
        return await _context.Empresas
            .AsNoTracking()
            .Where(e => e.EmpresaId == id)
            .Select(e => new EmpresaResponseDto
            {
                EmpresaId = e.EmpresaId,
                Empresa = e.Nome,
                CodigoAlternativo = e.CodigoAlternativo,
                Status = e.Status
            })
            .FirstOrDefaultAsync();
    }

    public async Task<EmpresaResponseDto> CreateAsync(EmpresaCreateDto dto)
    {
        var nomeEmUso = await _context.Empresas
            .AnyAsync(e => e.Nome == dto.Empresa);

        if (nomeEmUso)
        {
            throw new ConflictException($"Já existe uma empresa com o nome '{dto.Empresa}'.");
        }

        var codigoEmUso = await _context.Empresas
            .AnyAsync(e => e.CodigoAlternativo == dto.CodigoAlternativo);

        if (codigoEmUso)
        {
            throw new ConflictException($"Já existe uma empresa com o código alternativo '{dto.CodigoAlternativo}'.");
        }

        var entity = new Empresa
        {
            EmpresaId = Guid.NewGuid().ToString(),
            Nome = dto.Empresa,
            CodigoAlternativo = dto.CodigoAlternativo,
            Status = true
        };

        await _context.Empresas.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new EmpresaResponseDto
        {
            EmpresaId = entity.EmpresaId,
            Empresa = entity.Nome,
            CodigoAlternativo = entity.CodigoAlternativo,
            Status = entity.Status
        };
    }

    public async Task<EmpresaResponseDto> UpdateAsync(string id, EmpresaUpdateDto dto)
    {
        var entity = await _context.Empresas.FirstOrDefaultAsync(e => e.EmpresaId == id)
            ?? throw new NotFoundException($"Empresa '{id}' não encontrada.");

        var nomeEmUso = await _context.Empresas
            .AnyAsync(e => e.Nome == dto.Empresa && e.EmpresaId != id);

        if (nomeEmUso)
        {
            throw new ConflictException($"Já existe uma empresa com o nome '{dto.Empresa}'.");
        }

        var codigoEmUso = await _context.Empresas
            .AnyAsync(e => e.CodigoAlternativo == dto.CodigoAlternativo && e.EmpresaId != id);

        if (codigoEmUso)
        {
            throw new ConflictException($"Já existe uma empresa com o código alternativo '{dto.CodigoAlternativo}'.");
        }

        entity.Nome = dto.Empresa;
        entity.CodigoAlternativo = dto.CodigoAlternativo;
        entity.Status = dto.Status;

        await _context.SaveChangesAsync();

        return new EmpresaResponseDto
        {
            EmpresaId = entity.EmpresaId,
            Empresa = entity.Nome,
            CodigoAlternativo = entity.CodigoAlternativo,
            Status = entity.Status
        };
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.Empresas.FirstOrDefaultAsync(e => e.EmpresaId == id)
            ?? throw new NotFoundException($"Empresa '{id}' não encontrada.");

        entity.Status = false;
        await _context.SaveChangesAsync();
    }
}
