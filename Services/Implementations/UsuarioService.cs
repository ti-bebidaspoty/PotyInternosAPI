using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PotyInternosAPI.Data;
using PotyInternosAPI.DTOs.Aplicacoes;
using PotyInternosAPI.DTOs.Usuarios;
using PotyInternosAPI.Exceptions;
using PotyInternosAPI.Models;
using PotyInternosAPI.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

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

    public async Task<UsuarioPageDto> GetAllAsync(UsuarioQueryDto options)
    {
        var query = _context.Usuarios
            .AsNoTracking()
            .Include(u => u.Departamento)
            .Include(u => u.Empresa)
            .AsQueryable();

        if (!options.IncludeInactive)
        {
            query = query.Where(u => u.Status);
        }

        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var search = options.Search.Trim();
            query = query.Where(u => u.Nome.Contains(search)
                || u.NomeUsuario.Contains(search) || u.Departamento.Nome.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var page = Math.Min(options.Page, Math.Max(1, (int)Math.Ceiling((double)totalCount / options.PageSize)));
        var ordered = (options.SortBy, options.Descending) switch
        {
            ("usuario", false) => query.OrderBy(u => u.NomeUsuario),
            ("usuario", true) => query.OrderByDescending(u => u.NomeUsuario),
            ("departamento", false) => query.OrderBy(u => u.Departamento.Nome),
            ("departamento", true) => query.OrderByDescending(u => u.Departamento.Nome),
            ("perfil", false) => query.OrderByDescending(u => u.IsAdmin),
            ("perfil", true) => query.OrderBy(u => u.IsAdmin),
            ("status", false) => query.OrderByDescending(u => u.Status),
            ("status", true) => query.OrderBy(u => u.Status),
            (_, true) => query.OrderByDescending(u => u.Nome),
            _ => query.OrderBy(u => u.Nome)
        };
        var items = await ordered.ThenBy(u => u.UsuarioId)
            .Skip((page - 1) * options.PageSize)
            .Take(options.PageSize)
            .Select(u => new UsuarioResponseDto
            {
                UsuarioId = u.UsuarioId,
                Nome = u.Nome,
                Usuario = u.NomeUsuario,
                DepartamentoId = u.DepartamentoId,
                Departamento = u.Departamento.Nome,
                EmpresaId = u.EmpresaId,
                Empresa = u.Empresa.Nome,
                Status = u.Status,
                IsAdmin = u.IsAdmin
            })
            .ToListAsync();
        return new UsuarioPageDto { Items = items, Page = page, PageSize = options.PageSize, TotalCount = totalCount };
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
                EmpresaId = u.EmpresaId,
                Empresa = u.Empresa.Nome,
                Status = u.Status,
                IsAdmin = u.IsAdmin,
                Aplicacoes = u.UsuariosAplicacoes
                    .Select(ua => new AplicacaoResponseDto
                    {
                        AplicacaoId = ua.Aplicacao.AplicacaoId,
                        Aplicacao = ua.Aplicacao.Nome,
                        Status = ua.Aplicacao.Status,
                        CamposAdicionais = ua.Aplicacao.CamposAdicionais
                            .OrderBy(c => c.Ordem)
                            .Select(c => new AplicacaoCampoAdicionalResponseDto
                            {
                                CampoAdicionalId = c.CampoAdicionalId,
                                Nome = c.Nome,
                                Tipo = c.Tipo,
                                Ordem = c.Ordem
                            })
                            .ToList(),
                        ValoresCamposAdicionais = ua.CamposAdicionaisValores
                            .Select(v => new AplicacaoCampoAdicionalValorDto
                            {
                                CampoAdicionalId = v.CampoAdicionalId,
                                Valor = v.Valor
                            })
                            .ToList()
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

        var empresa = await _context.Empresas
            .FirstOrDefaultAsync(e => e.EmpresaId == dto.EmpresaId)
            ?? throw new ValidationException($"Empresa '{dto.EmpresaId}' não existe.");

        if (!empresa.Status)
        {
            throw new ValidationException("A empresa informada está inativa.");
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
            EmpresaId = dto.EmpresaId,
            Status = true,
            IsAdmin = dto.IsAdmin
        };

        var bytesSenha = Encoding.UTF8.GetBytes(dto.Senha);
        var hashBytes = SHA256.HashData(bytesSenha);
        entity.Senha = Convert.ToHexString(hashBytes);


        await _context.Usuarios.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new UsuarioResponseDto
        {
            UsuarioId = entity.UsuarioId,
            Nome = entity.Nome,
            Usuario = entity.NomeUsuario,
            DepartamentoId = entity.DepartamentoId,
            Departamento = departamento.Nome,
            EmpresaId = entity.EmpresaId,
            Empresa = empresa.Nome,
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

        var empresa = await _context.Empresas
            .FirstOrDefaultAsync(e => e.EmpresaId == dto.EmpresaId)
            ?? throw new ValidationException($"Empresa '{dto.EmpresaId}' não existe.");

        if (!empresa.Status)
        {
            throw new ValidationException("A empresa informada está inativa.");
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
        entity.EmpresaId = dto.EmpresaId;
        entity.Status = dto.Status;
        entity.IsAdmin = dto.IsAdmin;

        if (!string.IsNullOrWhiteSpace(dto.Senha))
        {
            var bytesSenha = Encoding.UTF8.GetBytes(dto.Senha);
            var hashBytes = SHA256.HashData(bytesSenha);
            entity.Senha = Convert.ToHexString(hashBytes);
        }

        await _context.SaveChangesAsync();

        return new UsuarioResponseDto
        {
            UsuarioId = entity.UsuarioId,
            Nome = entity.Nome,
            Usuario = entity.NomeUsuario,
            DepartamentoId = entity.DepartamentoId,
            Departamento = departamento.Nome,
            EmpresaId = entity.EmpresaId,
            Empresa = empresa.Nome,
            Status = entity.Status,
            IsAdmin = entity.IsAdmin
        };
    }

    public async Task ChangePasswordAsync(string id, UsuarioChangePasswordDto dto)
    {
        var entity = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == id)
            ?? throw new NotFoundException($"Usuário '{id}' não encontrado.");
        var bytesSenha = Encoding.UTF8.GetBytes(dto.Senha);
        var hashBytes = SHA256.HashData(bytesSenha);
        entity.Senha = Convert.ToHexString(hashBytes);
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
                Status = ua.Aplicacao.Status,
                CamposAdicionais = ua.Aplicacao.CamposAdicionais
                    .OrderBy(c => c.Ordem)
                    .Select(c => new AplicacaoCampoAdicionalResponseDto
                    {
                        CampoAdicionalId = c.CampoAdicionalId,
                        Nome = c.Nome,
                        Tipo = c.Tipo,
                        Ordem = c.Ordem
                    })
                    .ToList(),
                ValoresCamposAdicionais = ua.CamposAdicionaisValores
                    .Select(v => new AplicacaoCampoAdicionalValorDto
                    {
                        CampoAdicionalId = v.CampoAdicionalId,
                        Valor = v.Valor
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task VincularAplicacaoAsync(string usuarioId, string aplicacaoId, VincularAplicacaoDto? dto = null)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId)
            ?? throw new NotFoundException($"Usuário '{usuarioId}' não encontrado.");

        if (!usuario.Status)
        {
            throw new ValidationException("O usuário informado está inativo.");
        }

        var aplicacao = await _context.Aplicacoes
            .Include(a => a.CamposAdicionais)
            .FirstOrDefaultAsync(a => a.AplicacaoId == aplicacaoId)
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

        var vinculo = new UsuariosAplicacao
        {
            UsuarioId = usuarioId,
            AplicacaoId = aplicacaoId,
            CamposAdicionaisValores = MontarValoresCampos(usuarioId, aplicacao, dto?.CamposAdicionais ?? new())
        };

        await _context.UsuariosAplicacoes.AddAsync(vinculo);

        await _context.SaveChangesAsync();
    }

    public async Task AtualizarCamposAplicacaoAsync(string usuarioId, string aplicacaoId, AtualizarCamposAplicacaoDto dto)
    {
        var vinculo = await _context.UsuariosAplicacoes
            .Include(ua => ua.Aplicacao)
                .ThenInclude(a => a.CamposAdicionais)
            .Include(ua => ua.CamposAdicionaisValores)
            .FirstOrDefaultAsync(ua => ua.UsuarioId == usuarioId && ua.AplicacaoId == aplicacaoId)
            ?? throw new NotFoundException("Vínculo entre usuário e aplicação não encontrado.");

        var novosValores = MontarValoresCampos(usuarioId, vinculo.Aplicacao, dto.CamposAdicionais);

        _context.UsuariosAplicacoesCamposAdicionaisValores.RemoveRange(vinculo.CamposAdicionaisValores);
        await _context.UsuariosAplicacoesCamposAdicionaisValores.AddRangeAsync(novosValores);
        await _context.SaveChangesAsync();
    }

    public async Task DesvincularAplicacaoAsync(string usuarioId, string aplicacaoId)
    {
        var vinculo = await _context.UsuariosAplicacoes
            .Include(ua => ua.CamposAdicionaisValores)
            .FirstOrDefaultAsync(ua => ua.UsuarioId == usuarioId && ua.AplicacaoId == aplicacaoId)
            ?? throw new NotFoundException("Vínculo entre usuário e aplicação não encontrado.");

        _context.UsuariosAplicacoesCamposAdicionaisValores.RemoveRange(vinculo.CamposAdicionaisValores);
        _context.UsuariosAplicacoes.Remove(vinculo);
        await _context.SaveChangesAsync();
    }

    private static List<UsuarioAplicacaoCampoAdicionalValor> MontarValoresCampos(
        string usuarioId,
        Aplicacao aplicacao,
        List<AplicacaoCampoAdicionalValorDto> valoresInformados)
    {
        var valoresPorCampo = new Dictionary<string, object?>();
        foreach (var valor in valoresInformados.Where(v => !string.IsNullOrWhiteSpace(v.CampoAdicionalId)))
        {
            if (!valoresPorCampo.TryAdd(valor.CampoAdicionalId, valor.Valor))
            {
                throw new ConflictException($"O campo adicional '{valor.CampoAdicionalId}' foi informado mais de uma vez.");
            }
        }

        var valores = new List<UsuarioAplicacaoCampoAdicionalValor>();
        var camposDaAplicacao = aplicacao.CamposAdicionais.Select(c => c.CampoAdicionalId).ToHashSet();
        var campoDesconhecido = valoresPorCampo.Keys.FirstOrDefault(campoId => !camposDaAplicacao.Contains(campoId));
        if (campoDesconhecido is not null)
        {
            throw new ValidationException($"O campo adicional '{campoDesconhecido}' não pertence à aplicação '{aplicacao.AplicacaoId}'.");
        }

        foreach (var campo in aplicacao.CamposAdicionais.OrderBy(c => c.Ordem))
        {
            valoresPorCampo.TryGetValue(campo.CampoAdicionalId, out var valorInformado);

            string valorNormalizado;
            try
            {
                valorNormalizado = AplicacaoService.NormalizarValor(campo.Tipo, valorInformado);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                throw new ValidationException($"Valor inválido para o campo adicional '{campo.Nome}' ({campo.Tipo}).");
            }

            valores.Add(new UsuarioAplicacaoCampoAdicionalValor
            {
                UsuarioId = usuarioId,
                AplicacaoId = aplicacao.AplicacaoId,
                CampoAdicionalId = campo.CampoAdicionalId,
                Valor = valorNormalizado
            });
        }

        return valores;
    }
}
