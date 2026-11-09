using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KambaDiami.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace KambaDiami.Infrastructure.Locations;

/// <summary>
/// Consome a API angolaprovinciasapi.ao. Um único pedido a /provinces traz as províncias, os municípios e as
/// comunas; a resposta fica em memória durante algumas horas, porque a API demora perto de 2 segundos e só
/// aceita 60 pedidos por minuto. Se a API falhar, continua a usar a última resposta válida.
/// </summary>
public class AngolaLocationsClient : IAngolaLocationsProvider
{
    private const string FreshKey = "kd:angola-locations:fresh";
    private const string LastGoodKey = "kd:angola-locations:last-good";
    private static readonly TimeSpan FreshFor = TimeSpan.FromHours(6);
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AngolaLocationsClient> _logger;

    public AngolaLocationsClient(HttpClient http, IMemoryCache cache, ILogger<AngolaLocationsClient> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AngolaProvince>> GetProvincesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(FreshKey, out IReadOnlyList<AngolaProvince>? fresh) && fresh is not null)
            return fresh;

        // Só um pedido de cada vez vai à API; os outros esperam e usam a mesma resposta.
        await RefreshLock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(FreshKey, out fresh) && fresh is not null)
                return fresh;

            try
            {
                var provinces = await FetchAsync(ct);
                _cache.Set(FreshKey, provinces, FreshFor);
                _cache.Set(LastGoodKey, provinces, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
                return provinces;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException
                                           or System.Text.Json.JsonException or AngolaLocationsUnavailableException
                                       && !ct.IsCancellationRequested)
            {
                if (_cache.TryGetValue(LastGoodKey, out IReadOnlyList<AngolaProvince>? lastGood) && lastGood is not null)
                {
                    _logger.LogWarning(ex, "A API de províncias falhou; a usar a última resposta guardada.");
                    // Volta a tentar dentro de alguns minutos, em vez de a cada pedido.
                    _cache.Set(FreshKey, lastGood, TimeSpan.FromMinutes(5));
                    return lastGood;
                }

                _logger.LogError(ex, "A API de províncias não respondeu e não há dados guardados.");
                throw new AngolaLocationsUnavailableException(
                    "Não foi possível obter a lista de províncias e municípios. Tenta de novo dentro de momentos.", ex);
            }
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    public async Task<(AngolaProvince Province, AngolaMunicipality Municipality)?> FindMunicipalityAsync(
        string municipalitySlug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(municipalitySlug)) return null;

        foreach (var province in await GetProvincesAsync(ct))
        {
            var municipality = province.Municipalities.FirstOrDefault(m =>
                string.Equals(m.Slug, municipalitySlug, StringComparison.OrdinalIgnoreCase));
            if (municipality is not null) return (province, municipality);
        }
        return null;
    }

    private async Task<IReadOnlyList<AngolaProvince>> FetchAsync(CancellationToken ct)
    {
        var response = await _http.GetFromJsonAsync<ApiResponse<List<ApiProvince>>>("provinces", ct);
        if (response is not { Success: true, Data: { Count: > 0 } data })
            throw new AngolaLocationsUnavailableException("A API de províncias devolveu uma resposta vazia ou inválida.");

        return data
            .Where(p => !string.IsNullOrWhiteSpace(p.Slug) && !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new AngolaProvince(
                p.Slug!,
                p.Name!.Trim(),
                (p.Municipalities ?? [])
                    .Where(m => !string.IsNullOrWhiteSpace(m.Slug) && !string.IsNullOrWhiteSpace(m.Name))
                    .Select(m => new AngolaMunicipality(
                        m.Slug!,
                        m.Name!.Trim(),
                        (m.Communes ?? [])
                            .Where(c => !string.IsNullOrWhiteSpace(c.Slug) && !string.IsNullOrWhiteSpace(c.Name))
                            .Select(c => new AngolaCommune(c.Slug!, c.Name!.Trim()))
                            .OrderBy(c => c.Name, StringComparer.CurrentCulture)
                            .ToList()))
                    .OrderBy(m => m.Name, StringComparer.CurrentCulture)
                    .ToList()))
            .OrderBy(p => p.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    private sealed class ApiResponse<T>
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("data")] public T? Data { get; set; }
    }

    private sealed class ApiProvince
    {
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("municipalities")] public List<ApiMunicipality>? Municipalities { get; set; }
    }

    private sealed class ApiMunicipality
    {
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("communes")] public List<ApiCommune>? Communes { get; set; }
    }

    private sealed class ApiCommune
    {
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}
