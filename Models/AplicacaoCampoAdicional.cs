namespace PotyInternosAPI.Models;

public class AplicacaoCampoAdicional
{
    public string CampoAdicionalId { get; set; } = null!;

    public string AplicacaoId { get; set; } = null!;

    public string Nome { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public int Ordem { get; set; }

    public virtual Aplicacao Aplicacao { get; set; } = null!;

    public virtual ICollection<UsuarioAplicacaoCampoAdicionalValor> Valores { get; set; } = new List<UsuarioAplicacaoCampoAdicionalValor>();
}
