namespace KambaDiami.Api.Services;

/// <summary>
/// Confirma o tipo de um ficheiro pelos primeiros bytes ("assinatura"), em vez de confiar no nome
/// ou no tipo declarado pelo navegador, que podem ser forjados.
/// </summary>
public static class FileSignatures
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Pdf = "%PDF-"u8.ToArray();

    /// <summary>".jpg" ou ".png" se o conteúdo for uma imagem desses tipos; caso contrário null.</summary>
    public static async Task<string?> DetectImageExtensionAsync(IFormFile file)
    {
        var header = await ReadHeaderAsync(file, Png.Length);
        if (StartsWith(header, Jpeg)) return ".jpg";
        if (StartsWith(header, Png)) return ".png";
        return null;
    }

    public static async Task<bool> IsPdfAsync(IFormFile file) =>
        StartsWith(await ReadHeaderAsync(file, Pdf.Length), Pdf);

    private static async Task<byte[]> ReadHeaderAsync(IFormFile file, int length)
    {
        var buffer = new byte[length];
        await using var stream = file.OpenReadStream();
        var read = 0;
        while (read < length)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(read, length - read));
            if (n == 0) break;
            read += n;
        }
        return read == length ? buffer : buffer[..read];
    }

    private static bool StartsWith(byte[] data, byte[] prefix) =>
        data.Length >= prefix.Length && data.AsSpan(0, prefix.Length).SequenceEqual(prefix);
}
