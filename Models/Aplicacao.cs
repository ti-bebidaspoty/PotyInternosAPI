namespace PotyInternosAPI.Models;

public class Aplicacao
{
    public string AplicacaoId { get; set; } = null!;

    public string Nome { get; set; } = null!;

    public bool Status { get; set; }

    public virtual ICollection<AplicacaoCampoAdicional> CamposAdicionais { get; set; } = new List<AplicacaoCampoAdicional>();

    public virtual ICollection<UsuariosAplicacao> UsuariosAplicacoes { get; set; } = new List<UsuariosAplicacao>();
}
