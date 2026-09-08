using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PotyInternosAPI.Data;
using PotyInternosAPI.DTOs.Aplicacoes;
using PotyInternosAPI.DTOs.Usuarios;
using PotyInternosAPI.Exceptions;
using PotyInternosAPI.Models;
using PotyInternosAPI.Services.Interfaces;

namespace PotyInternosAPI.Services.Implementations;

public class UsuarioService : IUsuarioService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuarioService(AppDbContext context, IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Usuarios
            .AsNoTracking()
            .Include(u => u.Departamento)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(u => u.Status);
        }

        return await query
            .Select(u => new UsuarioResponseDto
            {
                UsuarioId = u.UsuarioId,
                Nome = u.Nome,
                Usuario = u.NomeUsuario,
                DepartamentoId = u.DepartamentoId,
                Departamento = u.Departamento.Nome,
                Status = u.Status,
                IsAdmin = u.IsAdmin
            })
            .ToListAsync();
    }

    public async Task<UsuarioResponseDto?> GetByIdAsync(string id)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.UsuarioId == id)
            .Select(u => new UsuarioResponseDto
            {
                UsuarioId = u.UsuarioId,
                Nome = u.Nome,
                Usuario = u.NomeUsuario,
                DepartamentoId = u.DepartamentoId,
                Departamento = u.Departamento.Nome,
                Status = u.Status,
                IsAdmin = u.IsAdmin,
                Aplicacoes = u.UsuariosAplicacoes
                    .Select(ua => new AplicacaoResponseDto
                    {
                        AplicacaoId = ua.Aplicacao.AplicacaoId,
                        Aplicacao = ua.Aplicacao.Nome,
                        Status = ua.Aplicacao.Status
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto dto)
    {
        var departamento = await _context.Departamentos
            .FirstOrDefaultAsync(d => d.DepartamentoId == dto.DepartamentoId)
            ?? throw new ValidationException($"Departamento '{dto.DepartamentoId}' não existe.");

        if (!departamento.Status)
        {
            throw new ValidationException("O departamento informado está inativo.");
        }

        var nomeUsuarioEmUso = await _context.Usuarios
            .AnyAsync(u => u.NomeUsuario == dto.Usuario);

        if (nomeUsuarioEmUso)
        {
            throw new ConflictException($"O nome de usuário '{dto.Usuario}' já está em uso.");
        }

        var entity = new Usuario
        {
            UsuarioId = Guid.NewGuid().ToString(),
            Nome = dto.Nome,
            NomeUsuario = dto.Usuario,
            DepartamentoId = dto.DepartamentoId,
            Status = true,
            IsAdmin = dto.IsAdmin
        };

        entity.Senha = _passwordHasher.HashPassword(entity, dto.Senha);

        await _context.Usuarios.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new UsuarioResponseDto
        {
            UsuarioId = entity.UsuarioId,
            Nome = entity.Nome,
            Usuario = entity.NomeUsuario,
            DepartamentoId = entity.DepartamentoId,
            Departamento = departamento.Nome,
            Status = entity.Status,
            IsAdmin = entity.IsAdmin
        };
    }

    public async Task<UsuarioResponseDto> UpdateAsync(string id, UsuarioUpdateDto dto)
    {
        var entity = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id)
            ?? throw new NotFoundException($"Usuário '{id}' não encontrado.");

        var departamento = await _context.Departamentos
            .FirstOrDefaultAsync(d => d.DepartamentoId == dto.DepartamentoId)
            ?? throw new ValidationException($"Departamento '{dto.DepartamentoId}' não existe.");

        if (!departamento.Status)
        {
            throw new ValidationException("O departamento informado está inativo.");
        }

        var nomeUsuarioEmUso = await _context.Usuarios
            .AnyAsync(u => u.NomeUsuario == dto.Usuario && u.UsuarioId != id);

        if (nomeUsuarioEmUso)
        {
            throw new ConflictException($"O nome de usuário '{dto.Usuario}' já está em uso.");
        }

        entity.Nome = dto.Nome;
        entity.NomeUsuario = dto.Usuario;
        entity.DepartamentoId = dto.DepartamentoId;
        entity.Status = dto.Status;
        entity.IsAdmin = dto.IsAdmin;

        if (!string.IsNullOrWhiteSpace(dto.Senha))
        {
            entity.Senha = _passwordHasher.HashPassword(entity, dto.Senha);
        }

        await _context.SaveChangesAsync();

        return new UsuarioResponseDto
        {
            UsuarioId = entity.UsuarioId,
            Nome = entity.Nome,
            Usuario = entity.NomeUsuario,
            DepartamentoId = entity.DepartamentoId,
            Departamento = departamento.Nome,
            Status = entity.Status,
            IsAdmin = entity.IsAdmin
        };
    }

    public async Task ChangePasswordAsync(string id, UsuarioChangePasswordDto dto)
    {
        var entity = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id)
            ?? throw new NotFoundException($"Usuário '{id}' não encontrado.");

        entity.Senha = _passwordHasher.HashPassword(entity, dto.Senha);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id)
            ?? throw new NotFoundException($"Usuário '{id}' não encontrado.");

        entity.Status = false;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AplicacaoResponseDto>> GetAplicacoesAsync(string usuarioId)
    {
        var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.UsuarioId == usuarioId);
        if (!usuarioExiste)
        {
            throw new NotFoundException($"Usuário '{usuarioId}' não encontrado.");
        }

        return await _context.UsuariosAplicacoes
            .AsNoTracking()
            .Where(ua => ua.UsuarioId == usuarioId)
            .Select(ua => new AplicacaoResponseDto
            {
                AplicacaoId = ua.Aplicacao.AplicacaoId,
                Aplicacao = ua.Aplicacao.Nome,
                Status = ua.Aplicacao.Status
            })
            .ToListAsync();
    }

    public async Task VincularAplicacaoAsync(string usuarioId, string aplicacaoId)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId)
            ?? throw new NotFoundException($"Usuário '{usuarioId}' não encontrado.");

        if (!usuario.Status)
        {
            throw new ValidationException("O usuário informado está inativo.");
        }

        var aplicacao = await _context.Aplicacoes.FirstOrDefaultAsync(a => a.AplicacaoId == aplicacaoId)
            ?? throw new NotFoundException($"Aplicação '{aplicacaoId}' não encontrada.");

        if (!aplicacao.Status)
        {
            throw new ValidationException("A aplicação informada está inativa.");
        }

        var jaExiste = await _context.UsuariosAplicacoes
            .AnyAsync(ua => ua.UsuarioId == usuarioId && ua.AplicacaoId == aplicacaoId);

        if (jaExiste)
        {
            throw new ConflictException("O vínculo entre usuário e aplicação já existe.");
        }

        await _context.UsuariosAplicacoes.AddAsync(new UsuariosAplicacao
        {
            UsuarioId = usuarioId,
            AplicacaoId = aplicacaoId
        });

        await _context.SaveChangesAsync();
    }

    public async Task DesvincularAplicacaoAsync(string usuarioId, string aplicacaoId)
    {
        var vinculo = await _context.UsuariosAplicacoes
            .FirstOrDefaultAsync(ua => ua.UsuarioId == usuarioId && ua.AplicacaoId == aplicacaoId)
            ?? throw new NotFoundException("Vínculo entre usuário e aplicação não encontrado.");

        _context.UsuariosAplicacoes.Remove(vinculo);
        await _context.SaveChangesAsync();
    }
}
