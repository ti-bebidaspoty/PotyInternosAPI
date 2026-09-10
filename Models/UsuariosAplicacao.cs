namespace PotyInternosAPI.Models;

public class UsuariosAplicacao
{
    public string UsuarioId { get; set; } = null!;

    public string AplicacaoId { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Aplicacao Aplicacao { get; set; } = null!;

    public virtual ICollection<UsuarioAplicacaoCampoAdicionalValor> CamposAdicionaisValores { get; set; } = new List<UsuarioAplicacaoCampoAdicionalValor>();
}
