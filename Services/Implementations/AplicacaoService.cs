using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PotyInternosAPI.Data;
using PotyInternosAPI.DTOs.Aplicacoes;
using PotyInternosAPI.Exceptions;
using PotyInternosAPI.Models;
using PotyInternosAPI.Services.Interfaces;

namespace PotyInternosAPI.Services.Implementations;

public class AplicacaoService : IAplicacaoService
{
    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Int",
        "String",
        "Bool",
        "Float"
    };

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
                Status = a.Status,
                CamposAdicionais = a.CamposAdicionais
                    .OrderBy(c => c.Ordem)
                    .Select(c => new AplicacaoCampoAdicionalResponseDto
                    {
                        CampoAdicionalId = c.CampoAdicionalId,
                        Nome = c.Nome,
                        Tipo = c.Tipo,
                        Ordem = c.Ordem
                    })
                    .ToList()
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
                Status = a.Status,
                CamposAdicionais = a.CamposAdicionais
                    .OrderBy(c => c.Ordem)
                    .Select(c => new AplicacaoCampoAdicionalResponseDto
                    {
                        CampoAdicionalId = c.CampoAdicionalId,
                        Nome = c.Nome,
                        Tipo = c.Tipo,
                        Ordem = c.Ordem
                    })
                    .ToList()
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
            Status = true,
            CamposAdicionais = MontarCampos(dto.CamposAdicionais)
        };

        await _context.Aplicacoes.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new AplicacaoResponseDto
        {
            AplicacaoId = entity.AplicacaoId,
            Aplicacao = entity.Nome,
            Status = entity.Status,
            CamposAdicionais = entity.CamposAdicionais
                .OrderBy(c => c.Ordem)
                .Select(c => new AplicacaoCampoAdicionalResponseDto
                {
                    CampoAdicionalId = c.CampoAdicionalId,
                    Nome = c.Nome,
                    Tipo = c.Tipo,
                    Ordem = c.Ordem
                })
                .ToList()
        };
    }

    public async Task<AplicacaoResponseDto> UpdateAsync(string id, AplicacaoUpdateDto dto)
    {
        var entity = await _context.Aplicacoes
            .Include(a => a.CamposAdicionais)
            .FirstOrDefaultAsync(a => a.AplicacaoId == id)
            ?? throw new NotFoundException($"Aplicação '{id}' não encontrada.");

        var nomeEmUso = await _context.Aplicacoes
            .AnyAsync(a => a.Nome == dto.Aplicacao && a.AplicacaoId != id);

        if (nomeEmUso)
        {
            throw new ConflictException($"Já existe uma aplicação com o nome '{dto.Aplicacao}'.");
        }

        entity.Nome = dto.Aplicacao;
        entity.Status = dto.Status;
        await AtualizarCamposAsync(entity, dto.CamposAdicionais);

        await _context.SaveChangesAsync();

        return new AplicacaoResponseDto
        {
            AplicacaoId = entity.AplicacaoId,
            Aplicacao = entity.Nome,
            Status = entity.Status,
            CamposAdicionais = entity.CamposAdicionais
                .OrderBy(c => c.Ordem)
                .Select(c => new AplicacaoCampoAdicionalResponseDto
                {
                    CampoAdicionalId = c.CampoAdicionalId,
                    Nome = c.Nome,
                    Tipo = c.Tipo,
                    Ordem = c.Ordem
                })
                .ToList()
        };
    }

    public async Task DeleteAsync(string id)
    {
        var entity = await _context.Aplicacoes.FirstOrDefaultAsync(a => a.AplicacaoId == id)
            ?? throw new NotFoundException($"Aplicação '{id}' não encontrada.");

        entity.Status = false;
        await _context.SaveChangesAsync();
    }

    public static string NormalizarTipo(string tipo)
    {
        var trimmed = tipo.Trim();
        return trimmed.ToLowerInvariant() switch
        {
            "int" => "Int",
            "string" => "String",
            "bool" => "Bool",
            "float" => "Float",
            _ => throw new ValidationException($"Tipo de campo adicional '{tipo}' inválido. Use Int, String, Bool ou Float.")
        };
    }

    public static string ValorPadrao(string tipo)
    {
        return NormalizarTipo(tipo) switch
        {
            "Int" => "0",
            "String" => string.Empty,
            "Bool" => "false",
            "Float" => "0",
            _ => string.Empty
        };
    }

    public static string NormalizarValor(string tipo, object? valor)
    {
        var tipoNormalizado = NormalizarTipo(tipo);
        if (valor is null)
        {
            return ValorPadrao(tipoNormalizado);
        }

        if (valor is JsonElement json)
        {
            return NormalizarJsonElement(tipoNormalizado, json);
        }

        return tipoNormalizado switch
        {
            "Int" => Convert.ToInt32(valor, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            "String" => valor.ToString() ?? string.Empty,
            "Bool" => Convert.ToBoolean(valor, CultureInfo.InvariantCulture).ToString().ToLowerInvariant(),
            "Float" => Convert.ToDouble(valor, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            _ => ValorPadrao(tipoNormalizado)
        };
    }

    private static string NormalizarJsonElement(string tipo, JsonElement json)
    {
        if (json.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return ValorPadrao(tipo);
        }

        return tipo switch
        {
            "Int" => json.ValueKind == JsonValueKind.Number && json.TryGetInt32(out var intValue)
                ? intValue.ToString(CultureInfo.InvariantCulture)
                : json.ValueKind == JsonValueKind.String
                    ? int.Parse(json.GetString() ?? string.Empty, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
                    : throw new FormatException("Valor inteiro inválido."),
            "String" => json.ValueKind == JsonValueKind.String ? json.GetString() ?? string.Empty : json.ToString(),
            "Bool" => json.ValueKind == JsonValueKind.True || json.ValueKind == JsonValueKind.False
                ? json.GetBoolean().ToString().ToLowerInvariant()
                : json.ValueKind == JsonValueKind.String
                    ? bool.Parse(json.GetString() ?? string.Empty).ToString().ToLowerInvariant()
                    : throw new FormatException("Valor booleano inválido."),
            "Float" => json.ValueKind == JsonValueKind.Number
                ? json.GetDouble().ToString(CultureInfo.InvariantCulture)
                : json.ValueKind == JsonValueKind.String
                    ? double.Parse(json.GetString() ?? string.Empty, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
                    : throw new FormatException("Valor decimal inválido."),
            _ => ValorPadrao(tipo)
        };
    }

    private static List<AplicacaoCampoAdicional> MontarCampos(IEnumerable<AplicacaoCampoAdicionalCreateUpdateDto> campos)
    {
        var camposList = campos.ToList();
        ValidarCampos(camposList);

        return camposList
            .Select((campo, index) => new AplicacaoCampoAdicional
            {
                CampoAdicionalId = Guid.NewGuid().ToString(),
                Nome = campo.Nome.Trim(),
                Tipo = NormalizarTipo(campo.Tipo),
                Ordem = campo.Ordem > 0 ? campo.Ordem : index
            })
            .ToList();
    }

    private async Task AtualizarCamposAsync(Aplicacao aplicacao, List<AplicacaoCampoAdicionalCreateUpdateDto> camposDto)
    {
        ValidarCampos(camposDto);

        var idsRecebidos = camposDto
            .Where(c => !string.IsNullOrWhiteSpace(c.CampoAdicionalId))
            .Select(c => c.CampoAdicionalId!)
            .ToHashSet();

        var removidos = aplicacao.CamposAdicionais
            .Where(c => !idsRecebidos.Contains(c.CampoAdicionalId))
            .ToList();

        if (removidos.Count > 0)
        {
            var idsRemovidos = removidos.Select(c => c.CampoAdicionalId).ToList();
            var valoresRemovidos = await _context.UsuariosAplicacoesCamposAdicionaisValores
                .Where(v => idsRemovidos.Contains(v.CampoAdicionalId))
                .ToListAsync();

            _context.UsuariosAplicacoesCamposAdicionaisValores.RemoveRange(valoresRemovidos);
            _context.AplicacoesCamposAdicionais.RemoveRange(removidos);
            foreach (var removido in removidos)
            {
                aplicacao.CamposAdicionais.Remove(removido);
            }
        }

        for (var index = 0; index < camposDto.Count; index++)
        {
            var campoDto = camposDto[index];
            var tipoNormalizado = NormalizarTipo(campoDto.Tipo);
            var existente = string.IsNullOrWhiteSpace(campoDto.CampoAdicionalId)
                ? null
                : aplicacao.CamposAdicionais.FirstOrDefault(c => c.CampoAdicionalId == campoDto.CampoAdicionalId);

            if (existente is null)
            {
                var novoCampo = new AplicacaoCampoAdicional
                {
                    CampoAdicionalId = Guid.NewGuid().ToString(),
                    AplicacaoId = aplicacao.AplicacaoId,
                    Nome = campoDto.Nome.Trim(),
                    Tipo = tipoNormalizado,
                    Ordem = campoDto.Ordem > 0 ? campoDto.Ordem : index
                };

                aplicacao.CamposAdicionais.Add(novoCampo);
                await CriarValoresPadraoParaVinculosExistentesAsync(aplicacao.AplicacaoId, novoCampo);
                continue;
            }

            if (!string.Equals(existente.Tipo, tipoNormalizado, StringComparison.OrdinalIgnoreCase))
            {
                await ValidarValoresExistentesAsync(existente.CampoAdicionalId, tipoNormalizado);
            }

            existente.Nome = campoDto.Nome.Trim();
            existente.Tipo = tipoNormalizado;
            existente.Ordem = campoDto.Ordem > 0 ? campoDto.Ordem : index;
        }
    }

    private async Task CriarValoresPadraoParaVinculosExistentesAsync(string aplicacaoId, AplicacaoCampoAdicional campo)
    {
        var vinculos = await _context.UsuariosAplicacoes
            .AsNoTracking()
            .Where(ua => ua.AplicacaoId == aplicacaoId)
            .Select(ua => ua.UsuarioId)
            .ToListAsync();

        foreach (var usuarioId in vinculos)
        {
            await _context.UsuariosAplicacoesCamposAdicionaisValores.AddAsync(new UsuarioAplicacaoCampoAdicionalValor
            {
                UsuarioId = usuarioId,
                AplicacaoId = aplicacaoId,
                CampoAdicionalId = campo.CampoAdicionalId,
                Valor = ValorPadrao(campo.Tipo)
            });
        }
    }

    private async Task ValidarValoresExistentesAsync(string campoAdicionalId, string novoTipo)
    {
        var valores = await _context.UsuariosAplicacoesCamposAdicionaisValores
            .AsNoTracking()
            .Where(v => v.CampoAdicionalId == campoAdicionalId)
            .Select(v => v.Valor)
            .ToListAsync();

        foreach (var valor in valores)
        {
            try
            {
                NormalizarValor(novoTipo, valor);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                throw new ValidationException("Não é possível alterar o tipo do campo adicional porque existem valores incompatíveis cadastrados.");
            }
        }
    }

    private static void ValidarCampos(List<AplicacaoCampoAdicionalCreateUpdateDto> campos)
    {
        var nomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var campo in campos)
        {
            if (string.IsNullOrWhiteSpace(campo.Nome))
            {
                throw new ValidationException("Informe o nome de todos os campos adicionais.");
            }

            if (string.IsNullOrWhiteSpace(campo.Tipo) || !TiposPermitidos.Contains(campo.Tipo.Trim()))
            {
                throw new ValidationException($"Tipo de campo adicional '{campo.Tipo}' inválido. Use Int, String, Bool ou Float.");
            }

            if (!nomes.Add(campo.Nome.Trim()))
            {
                throw new ConflictException($"O campo adicional '{campo.Nome}' está duplicado na aplicação.");
            }
        }
    }
}
