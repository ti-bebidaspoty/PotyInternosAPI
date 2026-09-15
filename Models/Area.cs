namespace PotyInternosAPI.Models;

public class Area
{
    public string AreaId { get; set; } = null!;

    public string Nome { get; set; } = null!;

    public bool Status { get; set; }

    public virtual ICollection<Departamento> Departamentos { get; set; } = new List<Departamento>();
}
