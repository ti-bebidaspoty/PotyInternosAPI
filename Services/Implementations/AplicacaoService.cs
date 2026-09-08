using Microsoft.EntityFrameworkCore;
using PotyInternosAPI.Data;
using PotyInternosAPI.DTOs.Aplicacoes;
using PotyInternosAPI.Exceptions;
using PotyInternosAPI.Models;
using PotyInternosAPI.Services.Interfaces;

namespace PotyInternosAPI.Services.Implementations;

public class AplicacaoService : IAplicacaoService
{
    private readonly AppDbContext _context;

    public AplicacaoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AplicacaoResponseDto>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Aplicacoes.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(a => a.Status);
        }

        return await query
            .Select(a => new AplicacaoResponseDto
            {
                AplicacaoId = a.AplicacaoId,
                Aplicacao = a.Nome,
                Status = a.Status
            })
            .ToListAsync();
    }

    public async Task<AplicacaoResponseDto?> GetByIdAsync(string id)
    {
        return await _context.Aplicacoes
            .AsNoTracking()
            .Where(a => a.AplicacaoId == id)
            .Select(a => new AplicacaoResponseDto
            {
                AplicacaoId = a.AplicacaoId,
                Aplicacao = a.Nome,
                Status = a.Status
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AplicacaoResponseDto> CreateAsync(AplicacaoCreateDto dto)
    {
        var nomeEmUso = await _context.Aplicacoes
            .AnyAsync(a => a.Nome == dto.Aplicacao);

        if (nomeEmUso)
        {
            throw new ConflictException($"Já existe uma aplicação com o nome '{dto.Aplicacao}'.");
        }

        var entity = new Aplicacao
        {
            AplicacaoId = Guid.NewGuid().ToString(),
            Nome = dto.Aplicacao,
            Status = true
        };

        await _context.Aplicacoes.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new AplicacaoResponseDto
        {
            AplicacaoId = entity.AplicacaoId,
            Aplicacao = entity.Nome,
            Status = entity.Status
        };
    }

    public async Task<AplicacaoResponseDto> UpdateAsync(string id, AplicacaoUpdateDto dto)
    {
        var entity = await _context.Aplicacoes.FirstOrDefaultAsync(a => a.AplicacaoId == id)
            ?? throw new NotFoundException($"Aplicação '{id}' não encontrada.");

        var nomeEmUso = await _context.Aplicacoes
            .AnyAsync(a => a.Nome == dto.Aplicacao && a.AplicacaoId != id);

        if (nomeEmUso)
        {
            throw new ConflictException($"Já existe uma aplicação com o nome '{dto.Aplicacao}'.");
        }

        entity.Nome = dto.Aplicacao;
        entity.Status = dto.Status;

        await _context.SaveChangesAsync();

        return new AplicacaoResponseDto
        {
            AplicacaoId = entity.AplicacaoId,
            Aplicacao = entity.Nome,
            Status = entity.Status
        };
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.Aplicacoes.FirstOrDefaultAsync(a => a.AplicacaoId == id)
            ?? throw new NotFoundException($"Aplicação '{id}' não encontrada.");

        entity.Status = false;
        await _context.SaveChangesAsync();
    }
}
