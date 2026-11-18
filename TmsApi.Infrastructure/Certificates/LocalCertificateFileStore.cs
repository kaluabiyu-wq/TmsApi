using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using TmsApi.Application.Certificates;

namespace TmsApi.Infrastructure.Certificates;

public sealed class CertificateStorageOptions
{
    public string RootPath { get; set; } = "App_Data/certificates";
}

public sealed class LocalCertificateFileStore(
    IOptions<CertificateStorageOptions> options,
    IHostEnvironment environment) : ICertificateFileStore
{
    private string Root
    {
        get
        {
            var configured = options.Value.RootPath;
            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(environment.ContentRootPath, configured);
        }
    }

    public async Task<string> SaveAsync(string fileName, byte[] content, CancellationToken ct)
    {
        Directory.CreateDirectory(Root);
        var safeName = Path.GetFileName(fileName);
        var key = $"{Guid.NewGuid():N}_{safeName}";
        var fullPath = Path.GetFullPath(Path.Combine(Root, key));
        var rootFull = Path.GetFullPath(Root) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootFull, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid certificate storage key.");

        await File.WriteAllBytesAsync(fullPath, content, ct);
        return key;
    }

    public async Task<byte[]?> ReadAsync(string storageKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey != Path.GetFileName(storageKey))
            return null;

        var fullPath = Path.GetFullPath(Path.Combine(Root, storageKey));
        var rootFull = Path.GetFullPath(Root) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootFull, StringComparison.Ordinal))
            return null;

        return File.Exists(fullPath)
            ? await File.ReadAllBytesAsync(fullPath, ct)
            : null;
    }
}
