namespace PotyInternosAPI.Models;

public class Empresa
{
    public string EmpresaId { get; set; } = null!;

    public string Nome { get; set; } = null!;

    public string CodigoAlternativo { get; set; } = null!;

    public bool Status { get; set; }

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
