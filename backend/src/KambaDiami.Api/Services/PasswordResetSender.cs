using System.Net;
using System.Text;
using KambaDiami.Application.Abstractions;
using KambaDiami.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace KambaDiami.Api.Services;

/// <summary>Gera o token de recuperação e envia ao utilizador a ligação para a página /reset-password.</summary>
public class PasswordResetSender
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public PasswordResetSender(UserManager<ApplicationUser> userManager, IEmailSender emailSender, IConfiguration configuration)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public async Task SendAsync(ApplicationUser user, CancellationToken ct = default)
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var frontendUrl = (_configuration["App:FrontendBaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
        var link = $"{frontendUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encodedToken}";

        var body = $"""
            <p>Olá {WebUtility.HtmlEncode(user.FirstName)},</p>
            <p>Recebemos um pedido para definir uma nova palavra-passe na tua conta Kamba Diami.</p>
            <p><a href="{link}">Definir nova palavra-passe</a></p>
            <p>A ligação é válida durante 2 horas. Se não foste tu a pedir, ignora este email: a tua palavra-passe actual continua a funcionar.</p>
            """;

        await _emailSender.SendAsync(user.Email!, "Kamba Diami: recuperar a palavra-passe", body, ct);
    }
}
