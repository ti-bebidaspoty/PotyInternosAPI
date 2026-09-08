using System.ComponentModel.DataAnnotations;

namespace PotyInternosAPI.DTOs.Auth;

public class LoginRequestDto
{
    [Required]
    public string Usuario { get; set; } = null!;

    [Required]
    public string Senha { get; set; } = null!;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public string UsuarioId { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string Usuario { get; set; } = null!;
    public bool IsAdmin { get; set; }
}
