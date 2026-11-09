namespace PotyInternosAPI.Models;

public class AplicacaoVersao
{
    public string VersaoId { get; set; } = null!;
    public string AplicacaoId { get; set; } = null!;
    public string Versao { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public DateTime DataPublicacao { get; set; }
    public bool Android { get; set; }
    public bool Ios { get; set; }
    public bool Ativa { get; set; }
    public string? ApkArquivo { get; set; }
    public string? IpaArquivo { get; set; }
    public string? AndroidExternalUrl { get; set; }
    public string? IosExternalUrl { get; set; }
    public string? IosBundleId { get; set; }
    public int? AndroidVersionCode { get; set; }
    public string? IosBuildNumber { get; set; }
    public string? ApkSha256 { get; set; }
    public string? IpaSha256 { get; set; }
    public virtual Aplicacao Aplicacao { get; set; } = null!;
}
