using System.ComponentModel.DataAnnotations;

namespace PotyInternosAPI.DTOs.Aplicacoes;

public class AplicacaoResponseDto
{
    public string AplicacaoId { get; set; } = null!;
    public string Aplicacao { get; set; } = null!;
    public bool Status { get; set; }
}

public class AplicacaoCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Aplicacao { get; set; } = null!;
}

public class AplicacaoUpdateDto
{
    [Required]
    [MaxLength(200)]
    public string Aplicacao { get; set; } = null!;

    public bool Status { get; set; }
}
