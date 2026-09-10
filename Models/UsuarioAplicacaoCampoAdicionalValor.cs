namespace PotyInternosAPI.Models;

public class UsuarioAplicacaoCampoAdicionalValor
{
    public string UsuarioId { get; set; } = null!;

    public string AplicacaoId { get; set; } = null!;

    public string CampoAdicionalId { get; set; } = null!;

    public string Valor { get; set; } = null!;

    public virtual UsuariosAplicacao UsuarioAplicacao { get; set; } = null!;

    public virtual AplicacaoCampoAdicional CampoAdicional { get; set; } = null!;
}
