namespace KambaDiami.Application.Abstractions;

public interface IFileStorageService
{
    Task<string> SavePhotoAsync(Guid userId, Stream content, string fileName, CancellationToken ct = default);
    Task<string> SaveGeneratedContractAsync(int contractId, byte[] content, CancellationToken ct = default);
    Task<string> SaveSignedContractAsync(int contractId, string partyLabel, Stream content, string fileName, CancellationToken ct = default);
    string GetAbsolutePath(string relativePath);
}
