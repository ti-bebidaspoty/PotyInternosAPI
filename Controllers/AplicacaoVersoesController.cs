using System.Security;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PotyInternosAPI.DTOs.Aplicacoes;
using PotyInternosAPI.Exceptions;
using PotyInternosAPI.Services.Implementations;

namespace PotyInternosAPI.Controllers;

[ApiController]
[Route("api/aplicacoes/{aplicacaoId}/versoes")]
[Authorize(Roles = "Admin")]
public sealed class AplicacaoVersoesController : ControllerBase
{
    private readonly AplicacaoVersaoService _service;
    public AplicacaoVersoesController(AplicacaoVersaoService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<AplicacaoVersaoDto>>> List(string aplicacaoId) =>
        Ok(await _service.ListAsync(aplicacaoId));

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(3221225472)]
    [RequestFormLimits(MultipartBodyLengthLimit = 3221225472)]
    public async Task<ActionResult<AplicacaoVersaoDto>> Publish(string aplicacaoId, [FromForm] PublicarVersaoDto dto, CancellationToken cancellationToken)
    {
        var published = await _service.PublishAsync(aplicacaoId, dto, cancellationToken);
        return Ok(published);
    }

    [HttpPut("{versaoId}/ativar")]
    public async Task<ActionResult<AplicacaoVersaoDto>> Activate(string aplicacaoId, string versaoId, CancellationToken cancellationToken) =>
        Ok(await _service.ActivateAsync(aplicacaoId, versaoId, cancellationToken));
}

[ApiController]
[Route("api/apps/{appId}")]
public sealed class PublicAppsController : ControllerBase
{
    private readonly AplicacaoVersaoService _service;
    private readonly IConfiguration _configuration;
    public PublicAppsController(AplicacaoVersaoService service, IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet("atual")]
    public async Task<ActionResult<AplicacaoAtualDto>> Current(string appId, CancellationToken cancellationToken)
    {
        var (application, version) = await _service.CurrentAsync(appId, cancellationToken);
        var androidUrl = version.Android ? version.AndroidExternalUrl ?? Absolute($"api/apps/{application.AplicacaoId}/download/android") : null;
        var manifestUrl = version.IpaArquivo is not null ? Absolute($"api/apps/{application.AplicacaoId}/versions/{version.VersaoId}/manifest.plist") : null;
        return Ok(new AplicacaoAtualDto
        {
            AplicacaoId = application.AplicacaoId,
            Slug = application.Slug,
            Aplicacao = application.Nome,
            VersaoId = version.VersaoId,
            VersaoAtual = version.Versao,
            Descricao = version.Descricao,
            DataPublicacao = DateTime.SpecifyKind(version.DataPublicacao, DateTimeKind.Utc),
            Android = version.Android,
            Ios = version.Ios,
            AndroidVersionCode = version.AndroidVersionCode,
            IosBuildNumber = version.IosBuildNumber,
            AndroidUrl = androidUrl,
            IosManifestUrl = manifestUrl,
            IosUrl = version.IosExternalUrl ?? (manifestUrl is null ? null : "itms-services://?action=download-manifest&url=" + Uri.EscapeDataString(manifestUrl)),
            AndroidDistributionType = version.Android ? version.AndroidExternalUrl is null ? "apk" : "link" : null,
            IosDistributionType = version.Ios ? version.IosExternalUrl is null ? "ipa" : "link" : null
        });
    }

    [AllowAnonymous]
    [HttpGet("download/android")]
    public async Task<IActionResult> Android(string appId, CancellationToken cancellationToken)
    {
        var (_, version) = await _service.CurrentAsync(appId, cancellationToken);
        if (!version.Android) throw new NotFoundException("Android não está disponível nesta versão.");
        if (version.AndroidExternalUrl is not null) return Redirect(version.AndroidExternalUrl);
        var path = _service.ResolveFile(version.ApkArquivo!);
        if (!System.IO.File.Exists(path)) throw new NotFoundException("APK indisponível no servidor.");
        Response.Headers.CacheControl = "no-store";
        return PhysicalFile(path, "application/vnd.android.package-archive", $"{version.VersaoId}.apk", enableRangeProcessing: true);
    }

    [AllowAnonymous]
    [HttpGet("download/ios")]
    public async Task<IActionResult> Ios(string appId, CancellationToken cancellationToken)
    {
        var (application, version) = await _service.CurrentAsync(appId, cancellationToken);
        if (!version.Ios) throw new NotFoundException("iOS não está disponível nesta versão.");
        if (version.IosExternalUrl is not null) return Redirect(version.IosExternalUrl);
        var manifestUrl = Absolute($"api/apps/{application.AplicacaoId}/versions/{version.VersaoId}/manifest.plist");
        return Redirect("itms-services://?action=download-manifest&url=" + Uri.EscapeDataString(manifestUrl));
    }

    [AllowAnonymous]
    [HttpGet("versions/{versionId}/manifest.plist")]
    public async Task<IActionResult> Manifest(string appId, string versionId, CancellationToken cancellationToken)
    {
        var (application, version) = await _service.ByIdAsync(appId, versionId, cancellationToken);
        if (!version.Ios || version.IpaArquivo is null) throw new NotFoundException("IPA não está disponível nesta versão.");
        var ipa = Absolute($"api/apps/{application.AplicacaoId}/versions/{version.VersaoId}/app.ipa");
        if (!ipa.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Instalação iOS exige Distribution:PublicBaseUrl HTTPS.");
        var plist = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0"><dict><key>items</key><array><dict>
            <key>assets</key><array><dict><key>kind</key><string>software-package</string><key>url</key><string>{SecurityElement.Escape(ipa)}</string></dict></array>
            <key>metadata</key><dict>
            <key>bundle-identifier</key><string>{SecurityElement.Escape(version.IosBundleId)}</string>
            <key>bundle-version</key><string>{SecurityElement.Escape(version.IosBuildNumber)}</string>
            <key>kind</key><string>software</string>
            <key>title</key><string>{SecurityElement.Escape(application.Nome)}</string>
            </dict></dict></array></dict></plist>
            """;
        Response.Headers.CacheControl = "no-store";
        return Content(plist, "text/xml", Encoding.UTF8);
    }

    [AllowAnonymous]
    [HttpGet("versions/{versionId}/app.ipa")]
    public async Task<IActionResult> Ipa(string appId, string versionId, CancellationToken cancellationToken)
    {
        var (_, version) = await _service.ByIdAsync(appId, versionId, cancellationToken);
        if (!version.Ios || version.IpaArquivo is null) throw new NotFoundException("IPA não está disponível nesta versão.");
        var path = _service.ResolveFile(version.IpaArquivo!);
        if (!System.IO.File.Exists(path)) throw new NotFoundException("IPA indisponível no servidor.");
        Response.Headers.CacheControl = "no-store";
        return PhysicalFile(path, "application/octet-stream", enableRangeProcessing: true);
    }

    private string Absolute(string route)
    {
        var configured = _configuration["Distribution:PublicBaseUrl"];
        var baseUrl = string.IsNullOrWhiteSpace(configured)
            ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}/"
            : configured.TrimEnd('/') + "/";
        return new Uri(new Uri(baseUrl), route).AbsoluteUri;
    }
}
