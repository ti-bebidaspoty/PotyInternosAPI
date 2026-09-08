namespace PotyInternosAPI.Models;

public class Departamento
{
    public string DepartamentoId { get; set; } = null!;

    public string Nome { get; set; } = null!;

    public bool Status { get; set; }

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
