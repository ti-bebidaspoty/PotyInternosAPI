using System.ComponentModel.DataAnnotations;

namespace PotyInternosAPI.DTOs.Usuarios;

public class UsuarioQueryDto
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 25;
    public bool IncludeInactive { get; set; }
    public string? Search { get; set; }
    [RegularExpression("^(nome|usuario|departamento|perfil|status)$")]
    public string SortBy { get; set; } = "nome";
    public bool Descending { get; set; }
}

public class UsuarioPageDto
{
    public List<UsuarioResponseDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
