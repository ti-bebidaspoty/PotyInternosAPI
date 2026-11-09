using System.ComponentModel.DataAnnotations;

namespace PotyInternosAPI.DTOs.Aplicacoes;

public sealed class PublicarVersaoDto
{
    [Required, MaxLength(100)] public string Versao { get; set; } = null!;
    [Required, MaxLength(2000)] public string Descricao { get; set; } = null!;
    public bool Android { get; set; } = true;
    public bool Ios { get; set; } = true;
    public IFormFile? Apk { get; set; }
    public IFormFile? Ipa { get; set; }
    [MaxLength(200)] public string? IosBundleId { get; set; }
    public int? AndroidVersionCode { get; set; }
    [MaxLength(100)] public string? IosBuildNumber { get; set; }
}

public sealed class AplicacaoVersaoDto
{
    public string VersaoId { get; set; } = null!;
    public string Versao { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public DateTime DataPublicacao { get; set; }
    public bool Android { get; set; }
    public bool Ios { get; set; }
    public bool Ativa { get; set; }
    public string? IosBundleId { get; set; }
    public int? AndroidVersionCode { get; set; }
    public string? IosBuildNumber { get; set; }
}

public sealed class AplicacaoAtualDto
{
    public string AplicacaoId { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Aplicacao { get; set; } = null!;
    public string VersaoId { get; set; } = null!;
    public string VersaoAtual { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public DateTime DataPublicacao { get; set; }
    public bool Android { get; set; }
    public bool Ios { get; set; }
    public string? AndroidUrl { get; set; }
    public string? IosUrl { get; set; }
    public string? IosManifestUrl { get; set; }
    public int? AndroidVersionCode { get; set; }
    public string? IosBuildNumber { get; set; }
}
