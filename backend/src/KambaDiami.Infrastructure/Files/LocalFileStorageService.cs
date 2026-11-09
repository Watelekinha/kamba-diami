using KambaDiami.Application.Abstractions;
using Microsoft.Extensions.Hosting;

namespace KambaDiami.Infrastructure.Files;

/// <summary>
/// As fotos de perfil ficam em wwwroot/uploads e são servidas como ficheiros estáticos.
/// Os contratos têm dados pessoais e ficam em App_Data/private, fora de wwwroot: só se
/// chega a eles pelos endpoints de ContractsController, que confirmam que quem pede é parte do contrato.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private const string PublicPrefix = "/uploads/";

    private readonly string _publicRoot;
    private readonly string _privateRoot;

    public LocalFileStorageService(IHostEnvironment env)
    {
        _publicRoot = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        _privateRoot = Path.Combine(env.ContentRootPath, "App_Data", "private");
    }

    public async Task<string> SavePhotoAsync(Guid userId, Stream content, string fileName, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        var relativePath = Path.Combine("photos", $"{userId:N}-{Guid.NewGuid():N}{extension}");
        await WriteToDiskAsync(_publicRoot, relativePath, content, ct);
        return PublicPrefix + relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    public async Task<string> SaveGeneratedContractAsync(int contractId, byte[] content, CancellationToken ct = default)
    {
        var relativePath = Path.Combine("contracts", "generated", $"contrato-{contractId}-{Guid.NewGuid():N}.pdf");
        var fullPath = Path.Combine(_privateRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, content, ct);
        return ToPrivatePath(relativePath);
    }

    public async Task<string> SaveSignedContractAsync(int contractId, string partyLabel, Stream content, string fileName, CancellationToken ct = default)
    {
        var relativePath = Path.Combine("contracts", "signed", $"contrato-{contractId}-{partyLabel}-{Guid.NewGuid():N}.pdf");
        await WriteToDiskAsync(_privateRoot, relativePath, content, ct);
        return ToPrivatePath(relativePath);
    }

    public string GetAbsolutePath(string storedPath)
    {
        var isPublic = storedPath.StartsWith(PublicPrefix, StringComparison.OrdinalIgnoreCase);
        var root = isPublic ? _publicRoot : _privateRoot;
        var relative = isPublic ? storedPath[PublicPrefix.Length..] : storedPath.TrimStart('/');

        var fullPath = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Caminho de ficheiro inválido.");
        }
        return fullPath;
    }

    private static async Task WriteToDiskAsync(string root, string relativePath, Stream content, CancellationToken ct)
    {
        var fullPath = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);
    }

    private static string ToPrivatePath(string relativePath) => relativePath.Replace(Path.DirectorySeparatorChar, '/');
}
