using System.ComponentModel.DataAnnotations;

namespace PotyInternosAPI.DTOs.Departamentos;

public class DepartamentoResponseDto
{
    public string DepartamentoId { get; set; } = null!;
    public string Departamento { get; set; } = null!;
    public string AreaId { get; set; } = null!;
    public string? Area { get; set; }
    public bool Status { get; set; }
}

public class DepartamentoCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Departamento { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string AreaId { get; set; } = null!;
}

public class DepartamentoUpdateDto
{
    [Required]
    [MaxLength(200)]
    public string Departamento { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string AreaId { get; set; } = null!;

    public bool Status { get; set; }
}
