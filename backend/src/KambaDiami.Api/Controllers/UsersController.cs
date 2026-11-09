using System.Security.Claims;
using FluentValidation;
using KambaDiami.Api.Services;
using KambaDiami.Application.Abstractions;
using KambaDiami.Application.Dtos;
using KambaDiami.Application.Mapping;
using KambaDiami.Domain.Entities;
using KambaDiami.Infrastructure.Persistence;
using KambaDiami.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KambaDiami.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly KambaDiamiDbContext _db;
    private readonly IValidator<UpdateProfileRequest> _updateValidator;
    private readonly IValidator<ChangePasswordRequest> _passwordValidator;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        KambaDiamiDbContext db,
        IValidator<UpdateProfileRequest> updateValidator,
        IValidator<ChangePasswordRequest> passwordValidator)
    {
        _userManager = userManager;
        _db = db;
        _updateValidator = updateValidator;
        _passwordValidator = passwordValidator;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe()
    {
        var user = await LoadUserAsync(CurrentUserId);
        if (user is null) return NotFound();

        var (avg, count) = await GetRatingStatsAsync(user.Id);
        return Ok(user.ToProfileDto(avg, count));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserProfileDto>> GetById(Guid id)
    {
        var user = await LoadUserAsync(id);
        if (user is null) return NotFound();

        var (avg, count) = await GetRatingStatsAsync(user.Id);
        var dto = user.ToProfileDto(avg, count);

        // Perfil de outra pessoa: o email, o telefone e a rua não são públicos (Lei n.º 22/11). As partes de um
        // contrato vêem os contactos no próprio contrato.
        if (id != CurrentUserId && !User.IsInRole("Admin"))
        {
            dto.Email = string.Empty;
            dto.PhoneNumber = string.Empty;
            dto.Address.StreetDetail = null;
        }

        return Ok(dto);
    }

    [HttpGet("{id:guid}/ratings")]
    public async Task<ActionResult<List<RatingDto>>> GetRatings(Guid id)
    {
        var exists = await _db.Users.AnyAsync(u => u.Id == id);
        if (!exists) return NotFound();

        var ratings = await _db.Ratings
            .Include(r => r.RaterUser)
            .Where(r => r.RateeUserId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(ratings.Select(r => r.ToDto()).ToList());
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateProfileRequest request)
    {
        request.Email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        request.FirstName = request.FirstName?.Trim() ?? string.Empty;
        request.LastName = request.LastName?.Trim() ?? string.Empty;

        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var neighborhoodExists = await _db.Neighborhoods.AnyAsync(n => n.Id == request.NeighborhoodId);
        if (!neighborhoodExists) return BadRequest(new { message = "Bairro inválido." });

        var user = await _userManager.Users.Include(u => u.Address).FirstOrDefaultAsync(u => u.Id == CurrentUserId);
        if (user is null) return NotFound();

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailTaken = await _userManager.Users.AnyAsync(u => u.Email == request.Email && u.Id != user.Id);
            if (emailTaken) return Conflict(new { message = "Já existe uma conta com este email." });
            user.Email = request.Email;
            user.UserName = request.Email;
            user.EmailConfirmed = false;
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = AngolaPhone.Normalize(request.PhoneNumber);
        user.Address.NeighborhoodId = request.NeighborhoodId;
        user.Address.StreetDetail = string.IsNullOrWhiteSpace(request.StreetDetail) ? null : request.StreetDetail.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var error in update.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }
        await _db.SaveChangesAsync();

        var updated = await LoadUserAsync(user.Id);
        var (avg, count) = await GetRatingStatsAsync(user.Id);
        return Ok(updated!.ToProfileDto(avg, count));
    }

    [HttpPut("me/password")]
    public async Task<ActionResult<TokenResponse>> ChangePassword(
        ChangePasswordRequest request,
        [FromServices] ITokenService tokenService,
        [FromServices] IUserSessionService sessions)
    {
        var validation = await _passwordValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
        if (user is null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }

        // Mudar a palavra-passe muda o carimbo de segurança e termina as sessões noutros dispositivos.
        // Este dispositivo recebe um token novo para continuar com sessão iniciada.
        sessions.Invalidate(user.Id);
        var token = await tokenService.GenerateTokenAsync(user);
        return Ok(new TokenResponse { Token = token.Token, ExpiresAt = token.ExpiresAt });
    }

    [HttpPost("me/photo")]
    public async Task<ActionResult<UserProfileDto>> UploadPhoto(IFormFile file)
    {
        if (file.Length == 0) return BadRequest(new { message = "Ficheiro vazio." });
        if (file.Length > 5 * 1024 * 1024) return BadRequest(new { message = "Foto excede o limite de 5MB." });

        // O tipo declarado pelo navegador e o nome do ficheiro podem ser forjados: confirma-se pelo conteúdo
        // (assinatura dos primeiros bytes) e a extensão guardada é escolhida pelo servidor.
        var extension = await FileSignatures.DetectImageExtensionAsync(file);
        if (extension is null)
            return BadRequest(new { message = "Formato de imagem inválido (usa JPEG ou PNG)." });

        var storage = HttpContext.RequestServices.GetRequiredService<Application.Abstractions.IFileStorageService>();
        await using var stream = file.OpenReadStream();
        var path = await storage.SavePhotoAsync(CurrentUserId, stream, "foto" + extension);

        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
        user!.PhotoPath = path;
        await _userManager.UpdateAsync(user);

        var updated = await LoadUserAsync(user.Id);
        var (avg, count) = await GetRatingStatsAsync(user.Id);
        return Ok(updated!.ToProfileDto(avg, count));
    }

    private Task<ApplicationUser?> LoadUserAsync(Guid id) =>
        _db.Users
            .Include(u => u.FamiliaProfile)
            .Include(u => u.SecretariaProfile)
            .Include(u => u.Address).ThenInclude(a => a.Neighborhood).ThenInclude(n => n.Municipality).ThenInclude(m => m.Province)
            .FirstOrDefaultAsync(u => u.Id == id);

    private async Task<(double? avg, int count)> GetRatingStatsAsync(Guid userId)
    {
        var scores = await _db.Ratings.Where(r => r.RateeUserId == userId).Select(r => r.Score).ToListAsync();
        if (scores.Count == 0) return (null, 0);
        return (scores.Average(), scores.Count);
    }
}
