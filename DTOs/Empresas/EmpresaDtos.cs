using System.ComponentModel.DataAnnotations;

namespace PotyInternosAPI.DTOs.Empresas;

public class EmpresaResponseDto
{
    public string EmpresaId { get; set; } = null!;
    public string Empresa { get; set; } = null!;
    public string CodigoAlternativo { get; set; } = null!;
    public bool Status { get; set; }
}

public class EmpresaCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Empresa { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string CodigoAlternativo { get; set; } = null!;
}

public class EmpresaUpdateDto
{
    [Required]
    [MaxLength(200)]
    public string Empresa { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string CodigoAlternativo { get; set; } = null!;

    public bool Status { get; set; }
}
