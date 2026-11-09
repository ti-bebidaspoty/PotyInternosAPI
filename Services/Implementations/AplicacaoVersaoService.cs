using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PotyInternosAPI.Data;
using PotyInternosAPI.DTOs.Aplicacoes;
using PotyInternosAPI.Exceptions;
using PotyInternosAPI.Models;

namespace PotyInternosAPI.Services.Implementations;

public sealed class AplicacaoVersaoService
{
    private const long MaxFileBytes = 1024L * 1024 * 1024;
    private readonly AppDbContext _db;
    private readonly string _storageRoot;

    public AplicacaoVersaoService(AppDbContext db, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _db = db;
        var configured = configuration["Distribution:StoragePath"];
        _storageRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "..", "PotyInternosReleases")
            : configured);
        var relativeToPublish = Path.GetRelativePath(environment.ContentRootPath, _storageRoot);
        if (relativeToPublish == "." || relativeToPublish != ".." &&
            !relativeToPublish.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
            !Path.IsPathRooted(relativeToPublish))
            throw new InvalidOperationException("Distribution:StoragePath deve ficar fora do diretório publicado da API.");
    }

    public async Task<List<AplicacaoVersaoDto>> ListAsync(string applicationId)
    {
        if (!await _db.Aplicacoes.AnyAsync(a => a.AplicacaoId == applicationId))
            throw new NotFoundException("Aplicação não encontrada.");
        return await _db.AplicacoesVersoes.AsNoTracking()
            .Where(v => v.AplicacaoId == applicationId)
            .OrderByDescending(v => v.DataPublicacao)
            .Select(v => Map(v)).ToListAsync();
    }

    public async Task<AplicacaoVersaoDto> PublishAsync(string applicationId, PublicarVersaoDto input, CancellationToken cancellationToken)
    {
        Validate(input);
        var application = await _db.Aplicacoes.AsNoTracking()
            .SingleOrDefaultAsync(a => a.AplicacaoId == applicationId, cancellationToken)
            ?? throw new NotFoundException("Aplicação não encontrada.");
        if (!application.Status) throw new ValidationException("A aplicação está inativa.");

        Directory.CreateDirectory(_storageRoot);
        var id = Guid.NewGuid().ToString("N");
        var staged = new List<string>();
        var final = new List<string>();
        try
        {
            string? apkName = null, ipaName = null, apkHash = null, ipaHash = null;
            if (input.Android)
            {
                apkName = id + ".apk";
                (var temp, apkHash) = await StageAsync(input.Apk!, cancellationToken);
                staged.Add(temp);
                ValidateArchive(temp, false);
            }
            if (input.Ios)
            {
                ipaName = id + ".ipa";
                (var temp, ipaHash) = await StageAsync(input.Ipa!, cancellationToken);
                staged.Add(temp);
                ValidateArchive(temp, true);
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            if (await _db.AplicacoesVersoes.AnyAsync(v => v.AplicacaoId == applicationId && v.Versao == input.Versao.Trim(), cancellationToken))
                throw new ConflictException("Já existe uma publicação com esta versão para a aplicação.");

            for (var i = 0; i < staged.Count; i++)
            {
                var target = Path.Combine(_storageRoot, input.Android && i == 0 ? apkName! : ipaName!);
                File.Move(staged[i], target);
                final.Add(target);
            }
            staged.Clear();

            var current = await _db.AplicacoesVersoes.Where(v => v.AplicacaoId == applicationId && v.Ativa).ToListAsync(cancellationToken);
            foreach (var old in current) old.Ativa = false;
            // O índice filtrado de uma versão ativa exige desativação antes de inserir a próxima.
            await _db.SaveChangesAsync(cancellationToken);
            var version = new AplicacaoVersao
            {
                VersaoId = id,
                AplicacaoId = applicationId,
                Versao = input.Versao.Trim(),
                Descricao = input.Descricao.Trim(),
                DataPublicacao = DateTime.UtcNow,
                Android = input.Android,
                Ios = input.Ios,
                Ativa = true,
                ApkArquivo = apkName,
                IpaArquivo = ipaName,
                ApkSha256 = apkHash,
                IpaSha256 = ipaHash,
                IosBundleId = input.Ios ? input.IosBundleId!.Trim() : null,
                AndroidVersionCode = input.Android ? input.AndroidVersionCode : null,
                IosBuildNumber = input.Ios ? input.IosBuildNumber!.Trim() : null
            };
            _db.AplicacoesVersoes.Add(version);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(version);
        }
        catch
        {
            foreach (var path in staged.Concat(final)) if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }

    public async Task<AplicacaoVersaoDto> ActivateAsync(string applicationId, string versionId, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var target = await _db.AplicacoesVersoes.SingleOrDefaultAsync(v => v.AplicacaoId == applicationId && v.VersaoId == versionId, cancellationToken)
            ?? throw new NotFoundException("Versão não encontrada.");
        if (!await _db.Aplicacoes.AnyAsync(a => a.AplicacaoId == applicationId && a.Status, cancellationToken))
            throw new ValidationException("A aplicação está inativa.");
        if (target.Android && !File.Exists(ResolveFile(target.ApkArquivo!)) ||
            target.Ios && !File.Exists(ResolveFile(target.IpaArquivo!)))
            throw new ConflictException("Os arquivos desta versão não estão disponíveis no servidor.");
        if (!target.Ativa)
        {
            var current = await _db.AplicacoesVersoes.Where(v => v.AplicacaoId == applicationId && v.Ativa).ToListAsync(cancellationToken);
            foreach (var old in current) old.Ativa = false;
            await _db.SaveChangesAsync(cancellationToken);
            target.Ativa = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return Map(target);
    }

    public async Task<(Aplicacao Application, AplicacaoVersao Version)> CurrentAsync(string appId, CancellationToken cancellationToken)
    {
        var alias = appId.ToLowerInvariant();
        var application = await _db.Aplicacoes.AsNoTracking()
            .SingleOrDefaultAsync(a => a.AplicacaoId == appId && a.Status, cancellationToken)
            ?? await _db.Aplicacoes.AsNoTracking()
                .SingleOrDefaultAsync(a => a.Slug == alias && a.Status, cancellationToken)
            ?? throw new NotFoundException("Aplicação não encontrada.");
        var version = await _db.AplicacoesVersoes.AsNoTracking()
            .SingleOrDefaultAsync(v => v.AplicacaoId == application.AplicacaoId && v.Ativa, cancellationToken)
            ?? throw new NotFoundException("A aplicação ainda não possui versão publicada.");
        return (application, version);
    }

    public async Task<(Aplicacao Application, AplicacaoVersao Version)> ByIdAsync(string appId, string versionId, CancellationToken cancellationToken)
    {
        var alias = appId.ToLowerInvariant();
        var application = await _db.Aplicacoes.AsNoTracking()
            .SingleOrDefaultAsync(a => a.AplicacaoId == appId && a.Status, cancellationToken)
            ?? await _db.Aplicacoes.AsNoTracking()
                .SingleOrDefaultAsync(a => a.Slug == alias && a.Status, cancellationToken)
            ?? throw new NotFoundException("Aplicação não encontrada.");
        var version = await _db.AplicacoesVersoes.AsNoTracking()
            .SingleOrDefaultAsync(v => v.AplicacaoId == application.AplicacaoId && v.VersaoId == versionId, cancellationToken)
            ?? throw new NotFoundException("Versão não encontrada.");
        return (application, version);
    }

    public string ResolveFile(string name)
    {
        // Somente nomes gerados pelo servidor, nunca caminhos enviados pelo usuário.
        if (!Regex.IsMatch(name, "^[a-f0-9]{32}\\.(apk|ipa)$", RegexOptions.CultureInvariant))
            throw new ValidationException("Nome de arquivo inválido.");
        return Path.Combine(_storageRoot, name);
    }

    private static void Validate(PublicarVersaoDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Versao) || input.Versao.Trim().Length > 100)
            throw new ValidationException("Informe a versão (até 100 caracteres).");
        if (string.IsNullOrWhiteSpace(input.Descricao) || input.Descricao.Trim().Length > 2000)
            throw new ValidationException("Informe a descrição (até 2000 caracteres).");
        if (!input.Android && !input.Ios) throw new ValidationException("Selecione Android, iOS ou ambos.");
        if (input.Android != (input.Apk is not null) || input.Ios != (input.Ipa is not null))
            throw new ValidationException("Envie um APK para Android e um IPA para iOS, apenas para as plataformas selecionadas.");
        if (input.Android && (input.AndroidVersionCode is null or <= 0))
            throw new ValidationException("Informe o versionCode positivo do APK Android.");
        if (input.Ios && (string.IsNullOrWhiteSpace(input.IosBundleId) || !Regex.IsMatch(input.IosBundleId, "^[A-Za-z0-9-]+(\\.[A-Za-z0-9-]+)+$") ||
            string.IsNullOrWhiteSpace(input.IosBuildNumber) || !Regex.IsMatch(input.IosBuildNumber, "^[0-9]+(\\.[0-9]+){0,2}$")))
            throw new ValidationException("Informe o bundle ID e o número de build do IPA iOS.");
        foreach (var file in new[] { input.Apk, input.Ipa }.Where(f => f is not null))
        {
            if (file!.Length is <= 0 or > MaxFileBytes)
                throw new ValidationException("O arquivo deve ter entre 1 byte e 1 GB.");
        }
        if (input.Android && !string.Equals(Path.GetExtension(input.Apk!.FileName), ".apk", StringComparison.OrdinalIgnoreCase) ||
            input.Ios && !string.Equals(Path.GetExtension(input.Ipa!.FileName), ".ipa", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Use arquivos .apk e .ipa para as respectivas plataformas.");
    }

    private async Task<(string Path, string Sha256)> StageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_storageRoot, Guid.NewGuid().ToString("N") + ".upload");
        try
        {
            await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.Asynchronous);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using var source = file.OpenReadStream();
            var buffer = new byte[1024 * 1024];
            long length = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) != 0)
            {
                length += read;
                if (length > MaxFileBytes) throw new ValidationException("Arquivo excede o limite de 1 GB.");
                hash.AppendData(buffer, 0, read);
                await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
            if (length != file.Length) throw new ValidationException("Upload incompleto.");
            return (path, Convert.ToHexString(hash.GetHashAndReset()));
        }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
    }

    private static void ValidateArchive(string path, bool ios)
    {
        try
        {
            using var archive = ZipFile.OpenRead(path);
            var found = archive.Entries.Any(e => ios
                ? e.FullName.StartsWith("Payload/", StringComparison.Ordinal) && e.FullName.EndsWith(".app/Info.plist", StringComparison.Ordinal)
                : e.FullName.Equals("AndroidManifest.xml", StringComparison.Ordinal));
            if (!found) throw new ValidationException(ios ? "IPA sem Payload/*.app/Info.plist." : "APK sem AndroidManifest.xml.");
        }
        catch (InvalidDataException) { throw new ValidationException("Arquivo de instalação inválido ou corrompido."); }
    }

    private static AplicacaoVersaoDto Map(AplicacaoVersao v) => new()
    {
        VersaoId = v.VersaoId, Versao = v.Versao, Descricao = v.Descricao,
        DataPublicacao = DateTime.SpecifyKind(v.DataPublicacao, DateTimeKind.Utc), Android = v.Android, Ios = v.Ios,
        Ativa = v.Ativa, IosBundleId = v.IosBundleId,
        AndroidVersionCode = v.AndroidVersionCode, IosBuildNumber = v.IosBuildNumber
    };
}
