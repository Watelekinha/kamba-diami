namespace KambaDiami.Application.Abstractions;

/// <summary>
/// Divisão administrativa de Angola (províncias, municípios e comunas), lida da API pública
/// angolaprovinciasapi.ao. Os bairros não existem nessa API e continuam na base de dados da plataforma.
/// </summary>
public interface IAngolaLocationsProvider
{
    /// <summary>
    /// Todas as províncias, com os municípios e as comunas. Lança <see cref="AngolaLocationsUnavailableException"/>
    /// se a API não responder e ainda não houver uma resposta anterior guardada.
    /// </summary>
    Task<IReadOnlyList<AngolaProvince>> GetProvincesAsync(CancellationToken ct = default);

    /// <summary>O município com este slug e a província a que pertence, ou null se não existir.</summary>
    Task<(AngolaProvince Province, AngolaMunicipality Municipality)?> FindMunicipalityAsync(string municipalitySlug, CancellationToken ct = default);
}

public sealed record AngolaProvince(string Slug, string Name, IReadOnlyList<AngolaMunicipality> Municipalities);

public sealed record AngolaMunicipality(string Slug, string Name, IReadOnlyList<AngolaCommune> Communes);

public sealed record AngolaCommune(string Slug, string Name);

public sealed class AngolaLocationsUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);
