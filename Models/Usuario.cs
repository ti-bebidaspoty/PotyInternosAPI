namespace PotyInternosAPI.Models;

public class Usuario
{
    public string UsuarioId { get; set; } = null!;

    public string Nome { get; set; } = null!;

    public string NomeUsuario { get; set; } = null!;

    public string Senha { get; set; } = null!;

    public string DepartamentoId { get; set; } = null!;

    public bool Status { get; set; }

    public bool IsAdmin { get; set; }

    public virtual Departamento Departamento { get; set; } = null!;

    public virtual ICollection<UsuariosAplicacao> UsuariosAplicacoes { get; set; } = new List<UsuariosAplicacao>();
}
