using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using ErrorOr;
using Microsoft.Extensions.Options;

namespace OxyKit.Mail.Features.Templates;

public interface IMailTemplateStore
{
    IReadOnlyList<MailTemplateInfo> List();
    ErrorOr<MailTemplateDetail> Describe(string templateId);
    ErrorOr<MailRenderedTemplate> Render(string templateId, JsonElement? data);
    Task<ErrorOr<MailTemplateInfo>> SaveFromZipAsync(
        string templateId,
        Stream zipStream,
        CancellationToken ct = default
    );
    Task<ErrorOr<Success>> DeleteAsync(string templateId, CancellationToken ct = default);
    Task<ErrorOr<byte[]>> ExportZipAsync(string templateId, CancellationToken ct = default);
}

public sealed partial class MailTemplateStore : IMailTemplateStore
{
    private readonly string _rootPath;
    private readonly MailTemplateOptions _options;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public MailTemplateStore(IOptions<MailTemplateOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _rootPath = ResolveRoot(_options.TemplatesRoot, environment.ContentRootPath);
    }

    public IReadOnlyList<MailTemplateInfo> List()
    {
        if (!Directory.Exists(_rootPath))
            return [];

        var results = new List<MailTemplateInfo>();
        foreach (var dir in Directory.EnumerateDirectories(_rootPath))
        {
            var id = Path.GetFileName(dir);
            if (!SafeTemplateId().IsMatch(id))
                continue;
            if (
                File.Exists(Path.Combine(dir, "subject.txt"))
                && File.Exists(Path.Combine(dir, "body.html"))
            )
            {
                results.Add(new MailTemplateInfo(id));
            }
        }

        results.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        return results;
    }

    public ErrorOr<MailTemplateDetail> Describe(string templateId)
    {
        var idResult = NormalizeId(templateId);
        if (idResult.IsError)
            return idResult.Errors;

        var id = idResult.Value;
        var dirResult = ResolveTemplateDirectory(id);
        if (dirResult.IsError)
            return dirResult.Errors;

        var dir = dirResult.Value;
        if (
            !File.Exists(Path.Combine(dir, "subject.txt"))
            || !File.Exists(Path.Combine(dir, "body.html"))
        )
        {
            return Error.NotFound(description: $"Template '{id}' was not found.");
        }

        var files = EnumeratePackFiles(dir).OrderBy(f => f, StringComparer.Ordinal).ToArray();
        return new MailTemplateDetail(id, files);
    }

    public ErrorOr<MailRenderedTemplate> Render(string templateId, JsonElement? data)
    {
        var idResult = NormalizeId(templateId);
        if (idResult.IsError)
            return idResult.Errors;

        var id = idResult.Value;
        var dirResult = ResolveTemplateDirectory(id);
        if (dirResult.IsError)
            return dirResult.Errors;

        var dir = dirResult.Value;
        var subjectPath = Path.Combine(dir, "subject.txt");
        var bodyPath = Path.Combine(dir, "body.html");
        if (!File.Exists(subjectPath) || !File.Exists(bodyPath))
            return Error.NotFound(description: $"Template '{id}' was not found.");

        var subjectText = File.ReadAllText(subjectPath);
        var bodyText = File.ReadAllText(bodyPath);
        if (subjectText.Length > _options.MaxSubjectChars)
        {
            return Error.Validation(
                description: $"subject.txt exceeds the maximum length of {_options.MaxSubjectChars:N0} characters."
            );
        }

        if (bodyText.Length > _options.MaxBodyChars)
        {
            return Error.Validation(
                description: $"body.html exceeds the maximum length of {_options.MaxBodyChars:N0} characters."
            );
        }

        if (data is not { ValueKind: JsonValueKind.Object } payload)
        {
            return Error.Validation(
                description: "Field 'data' must be a JSON object when using templateId."
            );
        }

        try
        {
            var subject = MailTemplateRenderer
                .Render(subjectText, payload, htmlEncodeStrings: false)
                .Trim();
            if (subject.Length == 0)
                return Error.Validation(description: "Rendered subject is empty.");
            if (subject.Contains('\r') || subject.Contains('\n'))
            {
                return Error.Validation(
                    description: "Rendered subject must be a single line."
                );
            }

            var body = MailTemplateRenderer.Render(bodyText, payload, htmlEncodeStrings: true);
            return new MailRenderedTemplate(subject, body);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(description: ex.Message);
        }
    }

    public async Task<ErrorOr<MailTemplateInfo>> SaveFromZipAsync(
        string templateId,
        Stream zipStream,
        CancellationToken ct = default
    )
    {
        var idResult = NormalizeId(templateId);
        if (idResult.IsError)
            return idResult.Errors;

        var id = idResult.Value;
        await _writeGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(_rootPath);
            var staging = Path.Combine(_rootPath, $".incoming-{id}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(staging);

            try
            {
                var extract = await ExtractAndValidateZipAsync(zipStream, staging, ct)
                    .ConfigureAwait(false);
                if (extract.IsError)
                {
                    TryDeleteDirectory(staging);
                    return extract.Errors;
                }

                var target = Path.Combine(_rootPath, id);
                var backup = Path.Combine(_rootPath, $".backup-{id}-{Guid.NewGuid():N}");
                if (Directory.Exists(target))
                    Directory.Move(target, backup);

                try
                {
                    Directory.Move(staging, target);
                }
                catch
                {
                    if (Directory.Exists(backup) && !Directory.Exists(target))
                        Directory.Move(backup, target);
                    throw;
                }

                if (Directory.Exists(backup))
                    TryDeleteDirectory(backup);

                return new MailTemplateInfo(id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                TryDeleteDirectory(staging);
                return Error.Validation(description: "Failed to save the template pack.");
            }
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<ErrorOr<Success>> DeleteAsync(
        string templateId,
        CancellationToken ct = default
    )
    {
        var idResult = NormalizeId(templateId);
        if (idResult.IsError)
            return idResult.Errors;

        var id = idResult.Value;
        await _writeGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var dir = Path.GetFullPath(Path.Combine(_rootPath, id));
            if (!IsUnderRoot(dir))
                return Error.NotFound(description: $"Template '{id}' was not found.");

            if (
                !Directory.Exists(dir)
                || !File.Exists(Path.Combine(dir, "subject.txt"))
                || !File.Exists(Path.Combine(dir, "body.html"))
            )
            {
                return Error.NotFound(description: $"Template '{id}' was not found.");
            }

            TryDeleteDirectory(dir);
            return Result.Success;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<ErrorOr<byte[]>> ExportZipAsync(
        string templateId,
        CancellationToken ct = default
    )
    {
        var detail = Describe(templateId);
        if (detail.IsError)
            return detail.Errors;

        var dir = Path.Combine(_rootPath, detail.Value.Id);
        await using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var relative in detail.Value.Files)
            {
                ct.ThrowIfCancellationRequested();
                var fullPath = Path.Combine(dir, relative.Replace('/', Path.DirectorySeparatorChar));
                archive.CreateEntryFromFile(
                    fullPath,
                    relative.Replace('\\', '/'),
                    CompressionLevel.Optimal
                );
            }
        }

        return ms.ToArray();
    }

    private async Task<ErrorOr<Success>> ExtractAndValidateZipAsync(
        Stream zipStream,
        string staging,
        CancellationToken ct
    )
    {
        await using var buffer = new MemoryStream();
        await zipStream.CopyToAsync(buffer, ct).ConfigureAwait(false);
        if (buffer.Length == 0)
            return Error.Validation(description: "Template pack zip is empty.");
        if (buffer.Length > _options.MaxTemplatePackBytes)
        {
            return Error.Validation(
                description: $"Template pack exceeds the maximum size of {_options.MaxTemplatePackBytes:N0} bytes."
            );
        }

        buffer.Position = 0;
        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: false);
        var entries = archive
            .Entries.Where(e => !string.IsNullOrEmpty(e.FullName) && !e.FullName.EndsWith('/'))
            .ToList();
        if (entries.Count == 0)
            return Error.Validation(description: "Template pack zip has no files.");

        var stripPrefix = DetectSingleRootPrefix(entries.Select(e => e.FullName));
        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            string relative;
            try
            {
                relative = NormalizeZipEntryPath(entry.FullName, stripPrefix);
                ValidatePackRelativePath(relative);
            }
            catch (ArgumentException ex)
            {
                return Error.Validation(description: ex.Message);
            }

            var dest = Path.GetFullPath(
                Path.Combine(staging, relative.Replace('/', Path.DirectorySeparatorChar))
            );
            if (!IsUnderDirectory(staging, dest))
                return Error.Validation(description: $"Pack entry '{relative}' escapes the template folder.");

            var parent = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            await using var entryStream = entry.Open();
            await using var file = File.Create(dest);
            var copied = await CopyCappedAsync(
                    entryStream,
                    file,
                    MaxBytesForRelativePath(relative),
                    relative,
                    ct
                )
                .ConfigureAwait(false);
            if (copied.IsError)
                return copied.Errors;
        }

        if (!File.Exists(Path.Combine(staging, "subject.txt")))
            return Error.Validation(description: "Template pack must include subject.txt.");
        if (!File.Exists(Path.Combine(staging, "body.html")))
            return Error.Validation(description: "Template pack must include body.html.");

        var subject = await File.ReadAllTextAsync(Path.Combine(staging, "subject.txt"), ct)
            .ConfigureAwait(false);
        var body = await File.ReadAllTextAsync(Path.Combine(staging, "body.html"), ct)
            .ConfigureAwait(false);
        if (subject.Length > _options.MaxSubjectChars)
        {
            return Error.Validation(
                description: $"subject.txt exceeds the maximum length of {_options.MaxSubjectChars:N0} characters."
            );
        }

        if (body.Length > _options.MaxBodyChars)
        {
            return Error.Validation(
                description: $"body.html exceeds the maximum length of {_options.MaxBodyChars:N0} characters."
            );
        }

        return Result.Success;
    }

    private long MaxBytesForRelativePath(string relative)
    {
        if (relative.Equals("subject.txt", StringComparison.OrdinalIgnoreCase))
            return (long)_options.MaxSubjectChars * 4;
        if (relative.Equals("body.html", StringComparison.OrdinalIgnoreCase))
            return (long)_options.MaxBodyChars * 4;
        return 256 * 1024;
    }

    private static async Task<ErrorOr<Success>> CopyCappedAsync(
        Stream source,
        Stream destination,
        long maxBytes,
        string relative,
        CancellationToken ct
    )
    {
        var buffer = new byte[81920];
        long total = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (read == 0)
                return Result.Success;
            total += read;
            if (total > maxBytes)
            {
                return Error.Validation(
                    description: $"Pack entry '{relative}' exceeds the allowed size."
                );
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
        }
    }

    private static void ValidatePackRelativePath(string relative)
    {
        if (
            relative.Equals("subject.txt", StringComparison.OrdinalIgnoreCase)
            || relative.Equals("body.html", StringComparison.OrdinalIgnoreCase)
            || relative.Equals("contract.json", StringComparison.OrdinalIgnoreCase)
            || relative.Equals("sample-data.json", StringComparison.OrdinalIgnoreCase)
        )
        {
            return;
        }

        throw new ArgumentException(
            $"Pack entry '{relative}' is not allowed. Use subject.txt, body.html, optional contract.json / sample-data.json."
        );
    }

    private static string NormalizeZipEntryPath(string fullName, string? stripPrefix)
    {
        var name = fullName.Replace('\\', '/').Trim('/');
        if (name.Contains("..", StringComparison.Ordinal) || name.StartsWith('/'))
            throw new ArgumentException($"Pack entry '{fullName}' has an unsafe path.");

        if (!string.IsNullOrEmpty(stripPrefix))
        {
            if (!name.StartsWith(stripPrefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Pack entry '{fullName}' is outside the pack root folder.");
            name = name[stripPrefix.Length..];
        }

        if (string.IsNullOrWhiteSpace(name) || name.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException($"Pack entry '{fullName}' has an unsafe path.");
        if (!SafePackPath().IsMatch(name))
            throw new ArgumentException($"Pack entry '{name}' has an unsafe path.");
        return name;
    }

    private static string? DetectSingleRootPrefix(IEnumerable<string> fullNames)
    {
        var normalized = fullNames.Select(n => n.Replace('\\', '/').Trim('/')).ToArray();
        if (
            normalized.Any(n =>
                n.Equals("subject.txt", StringComparison.OrdinalIgnoreCase)
                || n.Equals("body.html", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return null;
        }

        string? prefix = null;
        foreach (var name in normalized)
        {
            var slash = name.IndexOf('/');
            if (slash <= 0)
                return null;
            var root = name[..(slash + 1)];
            if (prefix is null)
                prefix = root;
            else if (!string.Equals(prefix, root, StringComparison.OrdinalIgnoreCase))
                return null;
        }

        if (prefix is null)
            return null;

        var hasSubject = normalized.Any(n =>
            n.Equals(prefix + "subject.txt", StringComparison.OrdinalIgnoreCase)
        );
        var hasBody = normalized.Any(n =>
            n.Equals(prefix + "body.html", StringComparison.OrdinalIgnoreCase)
        );
        return hasSubject && hasBody ? prefix : null;
    }

    private static IEnumerable<string> EnumeratePackFiles(string dir)
    {
        var root = Path.GetFullPath(dir);
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (relative.StartsWith(".", StringComparison.Ordinal))
                continue;
            try
            {
                ValidatePackRelativePath(relative);
            }
            catch (ArgumentException)
            {
                continue;
            }

            yield return relative;
        }
    }

    private ErrorOr<string> NormalizeId(string? templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            return Error.Validation(description: "Field 'templateId' is required.");

        var id = templateId.Trim();
        if (!SafeTemplateId().IsMatch(id))
        {
            return Error.Validation(
                description: "Field 'templateId' must be a simple name (letters, digits, '-', '_')."
            );
        }

        return id;
    }

    private ErrorOr<string> ResolveTemplateDirectory(string id)
    {
        var candidate = Path.GetFullPath(Path.Combine(_rootPath, id));
        if (!IsUnderRoot(candidate))
            return Error.NotFound(description: $"Template '{id}' was not found.");
        if (!Directory.Exists(candidate))
            return Error.NotFound(description: $"Template '{id}' was not found.");
        return candidate;
    }

    private bool IsUnderRoot(string candidate)
    {
        var rootWithSep = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase)
            || string.Equals(candidate, _rootPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnderDirectory(string root, string candidate)
    {
        var fullRoot = Path.GetFullPath(root);
        var rootWithSep = fullRoot.EndsWith(Path.DirectorySeparatorChar)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;
        var fullCandidate = Path.GetFullPath(candidate);
        return fullCandidate.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase)
            || string.Equals(fullCandidate, fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRoot(string configured, string contentRoot)
    {
        if (string.IsNullOrWhiteSpace(configured))
            configured = "templates";
        return Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(contentRoot, configured));
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeTemplateId();

    [GeneratedRegex(
        @"^[A-Za-z0-9][A-Za-z0-9._/-]*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled
    )]
    private static partial Regex SafePackPath();
}
