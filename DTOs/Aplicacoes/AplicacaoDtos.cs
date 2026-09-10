using System.ComponentModel.DataAnnotations;

namespace PotyInternosAPI.DTOs.Aplicacoes;

public class AplicacaoResponseDto
{
    public string AplicacaoId { get; set; } = null!;
    public string Aplicacao { get; set; } = null!;
    public bool Status { get; set; }
    public List<AplicacaoCampoAdicionalResponseDto> CamposAdicionais { get; set; } = new();
    public List<AplicacaoCampoAdicionalValorDto> ValoresCamposAdicionais { get; set; } = new();
}

public class AplicacaoCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Aplicacao { get; set; } = null!;

    public List<AplicacaoCampoAdicionalCreateUpdateDto> CamposAdicionais { get; set; } = new();
}

public class AplicacaoUpdateDto
{
    [Required]
    [MaxLength(200)]
    public string Aplicacao { get; set; } = null!;

    public bool Status { get; set; }

    public List<AplicacaoCampoAdicionalCreateUpdateDto> CamposAdicionais { get; set; } = new();
}

public class AplicacaoCampoAdicionalResponseDto
{
    public string CampoAdicionalId { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public int Ordem { get; set; }
}

public class AplicacaoCampoAdicionalCreateUpdateDto
{
    public string? CampoAdicionalId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string Tipo { get; set; } = null!;

    public int Ordem { get; set; }
}

public class AplicacaoCampoAdicionalValorDto
{
    [Required]
    [MaxLength(50)]
    public string CampoAdicionalId { get; set; } = null!;

    public object? Valor { get; set; }
}

public class VincularAplicacaoDto
{
    public List<AplicacaoCampoAdicionalValorDto> CamposAdicionais { get; set; } = new();
}

public class AtualizarCamposAplicacaoDto
{
    public List<AplicacaoCampoAdicionalValorDto> CamposAdicionais { get; set; } = new();
}
