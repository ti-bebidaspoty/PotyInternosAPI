using System.ComponentModel.DataAnnotations;
using PotyInternosAPI.DTOs.Aplicacoes;

namespace PotyInternosAPI.DTOs.Usuarios;

public class UsuarioResponseDto
{
    public string UsuarioId { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string Usuario { get; set; } = null!;
    public string DepartamentoId { get; set; } = null!;
    public string? Departamento { get; set; }
    public string EmpresaId { get; set; } = null!;
    public string? Empresa { get; set; }
    public bool Status { get; set; }
    public bool IsAdmin { get; set; }
    public List<AplicacaoResponseDto> Aplicacoes { get; set; } = new();
}

public class UsuarioCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Usuario { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Senha { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string DepartamentoId { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string EmpresaId { get; set; } = null!;

    public bool IsAdmin { get; set; }
}

public class UsuarioUpdateDto
{
    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Usuario { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string DepartamentoId { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string EmpresaId { get; set; } = null!;

    public bool Status { get; set; }

    public bool IsAdmin { get; set; }

    /// <summary>
    /// Opcional. Quando informado, a senha do usuário será atualizada.
    /// Deixe nulo ou vazio para manter a senha atual.
    /// </summary>
    [MaxLength(200)]
    public string? Senha { get; set; }
}

public class UsuarioChangePasswordDto
{
    [Required]
    [MaxLength(200)]
    public string Senha { get; set; } = null!;
}
