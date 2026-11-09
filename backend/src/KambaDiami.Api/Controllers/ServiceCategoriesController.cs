using KambaDiami.Application.Dtos;
using KambaDiami.Domain.Entities;
using KambaDiami.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Api.Controllers;

[ApiController]
[Route("api/service-categories")]
public class ServiceCategoriesController : ControllerBase
{
    private readonly KambaDiamiDbContext _db;

    public ServiceCategoriesController(KambaDiamiDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ServiceCategoryDto>>> GetAll()
    {
        var categories = await _db.ServiceCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new ServiceCategoryDto { Id = c.Id, Name = c.Name })
            .ToListAsync();
        return Ok(categories);
    }

    [HttpPost]
    [Authorize(Roles = "Familia,Secretaria")]
    public async Task<ActionResult<ServiceCategoryDto>> Create(CreateServiceCategoryRequest request)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "O nome da categoria é obrigatório." });

        var existing = await _db.ServiceCategories.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        if (existing is not null)
            return Ok(new ServiceCategoryDto { Id = existing.Id, Name = existing.Name });

        var category = new ServiceCategory { Name = name, IsActive = true };
        _db.ServiceCategories.Add(category);
        await _db.SaveChangesAsync();

        return Ok(new ServiceCategoryDto { Id = category.Id, Name = category.Name });
    }
}
