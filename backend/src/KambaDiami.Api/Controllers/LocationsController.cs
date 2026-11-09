using System.Text.RegularExpressions;
using KambaDiami.Api.Services;
using KambaDiami.Application.Dtos;
using KambaDiami.Domain.Entities;
using KambaDiami.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Api.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly KambaDiamiDbContext _db;

    public LocationsController(KambaDiamiDbContext db)
    {
        _db = db;
    }

    [HttpGet("provinces")]
    public async Task<ActionResult<List<ProvinceDto>>> GetProvinces()
    {
        var provinces = await _db.Provinces
            .OrderBy(p => p.Name)
            .Select(p => new ProvinceDto { Id = p.Id, Name = p.Name })
            .ToListAsync();
        return Ok(provinces);
    }

    [HttpGet("provinces/{provinceId:int}/municipalities")]
    public async Task<ActionResult<List<MunicipalityDto>>> GetMunicipalities(int provinceId)
    {
        var municipalities = await _db.Municipalities
            .Where(m => m.ProvinceId == provinceId)
            .OrderBy(m => m.Name)
            .Select(m => new MunicipalityDto { Id = m.Id, Name = m.Name, ProvinceId = m.ProvinceId })
            .ToListAsync();
        return Ok(municipalities);
    }

    [HttpGet("municipalities/{municipalityId:int}/neighborhoods")]
    public async Task<ActionResult<List<NeighborhoodDto>>> GetNeighborhoods(int municipalityId)
    {
        var neighborhoods = await _db.Neighborhoods
            .Where(n => n.MunicipalityId == municipalityId)
            .OrderBy(n => n.Name)
            .Select(n => new NeighborhoodDto { Id = n.Id, Name = n.Name, MunicipalityId = n.MunicipalityId })
            .ToListAsync();
        return Ok(neighborhoods);
    }

    [HttpPost("municipalities")]
    // Sem sessão: quem se está a registar também precisa de acrescentar o seu bairro.
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Locations)]
    public async Task<ActionResult<MunicipalityDto>> CreateMunicipality(CreateMunicipalityRequest request)
    {
        var name = NormalizeName(request.Name);
        if (name is null)
            return BadRequest(new { message = InvalidNameMessage("município") });

        var provinceExists = await _db.Provinces.AnyAsync(p => p.Id == request.ProvinceId);
        if (!provinceExists)
            return BadRequest(new { message = "Província inválida." });

        var existing = await _db.Municipalities.FirstOrDefaultAsync(m =>
            m.ProvinceId == request.ProvinceId && m.Name.ToLower() == name.ToLower());
        if (existing is not null)
            return Ok(new MunicipalityDto { Id = existing.Id, Name = existing.Name, ProvinceId = existing.ProvinceId });

        var municipality = new Municipality { Name = name, ProvinceId = request.ProvinceId };
        _db.Municipalities.Add(municipality);
        await _db.SaveChangesAsync();

        return Ok(new MunicipalityDto { Id = municipality.Id, Name = municipality.Name, ProvinceId = municipality.ProvinceId });
    }

    [HttpPost("neighborhoods")]
    // Sem sessão: quem se está a registar também precisa de acrescentar o seu bairro.
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Locations)]
    public async Task<ActionResult<NeighborhoodDto>> CreateNeighborhood(CreateNeighborhoodRequest request)
    {
        var name = NormalizeName(request.Name);
        if (name is null)
            return BadRequest(new { message = InvalidNameMessage("bairro") });

        var municipalityExists = await _db.Municipalities.AnyAsync(m => m.Id == request.MunicipalityId);
        if (!municipalityExists)
            return BadRequest(new { message = "Município inválido." });

        var existing = await _db.Neighborhoods.FirstOrDefaultAsync(n =>
            n.MunicipalityId == request.MunicipalityId && n.Name.ToLower() == name.ToLower());
        if (existing is not null)
            return Ok(new NeighborhoodDto { Id = existing.Id, Name = existing.Name, MunicipalityId = existing.MunicipalityId });

        var neighborhood = new Neighborhood { Name = name, MunicipalityId = request.MunicipalityId };
        _db.Neighborhoods.Add(neighborhood);
        await _db.SaveChangesAsync();

        return Ok(new NeighborhoodDto { Id = neighborhood.Id, Name = neighborhood.Name, MunicipalityId = neighborhood.MunicipalityId });
    }

    private static readonly Regex AllowedName = new(@"^[\p{L}\p{N}][\p{L}\p{N} .'-]*$", RegexOptions.Compiled);

    /// <summary>Junta espaços repetidos e rejeita nomes vazios, demasiado curtos ou longos, ou com símbolos.</summary>
    private static string? NormalizeName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var name = Regex.Replace(raw.Trim(), @"\s+", " ");
        return name.Length is >= 2 and <= 80 && AllowedName.IsMatch(name) ? name : null;
    }

    private static string InvalidNameMessage(string what) =>
        $"Indica um nome de {what} válido, com 2 a 80 caracteres (letras, números, espaços, hífen ou apóstrofo).";
}
