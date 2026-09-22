namespace PotyInternosAPI.Models;

public class Departamento
{
    public string DepartamentoId { get; set; } = null!;

    public string Nome { get; set; } = null!;

    public string AreaId { get; set; } = null!;

    public bool Status { get; set; }
    public int? CodigoAlternativo { get; set; } = 0;

    public virtual Area Area { get; set; } = null!;

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
