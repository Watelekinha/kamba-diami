using System.Net;
using KambaDiami.Application.Abstractions;
using KambaDiami.Domain.Entities;
using KambaDiami.Domain.Enums;

namespace KambaDiami.Api.Services;

/// <summary>
/// Envia o email de boas-vindas depois do registo. Corre em segundo plano: o registo não espera pelo
/// envio e, se o email falhar, a conta fica criada na mesma (o erro só fica no log).
/// </summary>
public class WelcomeEmailSender
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WelcomeEmailSender> _logger;

    public WelcomeEmailSender(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<WelcomeEmailSender> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public void Queue(ApplicationUser user)
    {
        var email = user.Email!;
        var subject = $"Bem-vindo(a) à Kamba Diami, {user.FirstName}!";
        var body = BuildBody(user.FirstName, user.Role);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await sender.SendAsync(email, subject, body);
                _logger.LogInformation("Email de boas-vindas enviado para {Email}.", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Não foi possível enviar o email de boas-vindas para {Email}.", email);
            }
        });
    }

    private string BuildBody(string firstName, UserRole role)
    {
        var site = (_configuration["App:FrontendBaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
        var name = WebUtility.HtmlEncode(firstName);
        var logo = $"{site}/assets/logo/kamba-diami-180.png";

        var (intro, steps, ctaText, ctaPath) = role == UserRole.Familia
            ? ("A tua conta de <strong>família</strong> está pronta. Agora podes encontrar uma secretária do lar de confiança perto de ti.",
               new[]
               {
                   "Completa o teu perfil, para as secretárias saberem com quem vão falar.",
                   "Publica uma vaga ou procura secretárias disponíveis na tua zona.",
                   "Conversa pelo chat da plataforma e, quando chegarem a acordo, gera o contrato."
               },
               "Publicar a minha primeira vaga", "/job-postings/new")
            : ("A tua conta de <strong>secretária do lar</strong> está pronta. Agora podes encontrar famílias que procuram o teu trabalho.",
               new[]
               {
                   "Completa o teu perfil com foto e experiência: as famílias confiam mais em perfis completos.",
                   "Publica a tua disponibilidade ou responde às vagas perto de ti.",
                   "Conversa pelo chat da plataforma e assina um contrato que protege os teus direitos."
               },
               "Completar o meu perfil", "/profile");

        var stepsHtml = string.Concat(steps.Select((s, i) => $"""
            <tr>
              <td style="vertical-align:top;padding:0 12px 12px 0;">
                <div style="width:26px;height:26px;border-radius:13px;background:#e7f0fe;color:#1877f2;font-weight:700;font-size:13px;line-height:26px;text-align:center;">{i + 1}</div>
              </td>
              <td style="vertical-align:top;padding:3px 0 12px;font-size:15px;line-height:1.5;color:#1c1e21;">{s}</td>
            </tr>
            """));

        return $"""
            <!doctype html>
            <html lang="pt-PT">
            <body style="margin:0;padding:0;background:#f0f2f5;font-family:'Segoe UI',Arial,sans-serif;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f0f2f5;padding:24px 12px;">
                <tr><td align="center">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background:#ffffff;border-radius:16px;overflow:hidden;">
                    <tr>
                      <td style="background:linear-gradient(120deg,#2f8a5c,#1877f2);background-color:#1877f2;padding:28px 32px;">
                        <table role="presentation" cellpadding="0" cellspacing="0"><tr>
                          <td style="background:#ffffff;border-radius:12px;padding:6px;"><img src="{logo}" width="40" height="40" alt="" style="display:block;"></td>
                          <td style="padding-left:12px;color:#ffffff;font-family:Georgia,'Times New Roman',serif;font-size:24px;font-weight:bold;">Kamba Diami</td>
                        </tr></table>
                        <p style="margin:14px 0 0;color:#e6f0ff;font-size:13px;letter-spacing:2px;text-transform:uppercase;">O teu amigo em casa</p>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:32px;">
                        <h1 style="margin:0 0 12px;font-family:Georgia,'Times New Roman',serif;font-size:28px;color:#1c1e21;">Olá {name}, bem-vindo(a)!</h1>
                        <p style="margin:0 0 22px;font-size:15px;line-height:1.6;color:#4b4f56;">Obrigado por te registares na Kamba Diami. {intro}</p>
                        <p style="margin:0 0 12px;font-size:15px;font-weight:bold;color:#1c1e21;">Os próximos passos:</p>
                        <table role="presentation" cellpadding="0" cellspacing="0">{stepsHtml}</table>
                        <table role="presentation" cellpadding="0" cellspacing="0" style="margin:18px 0 6px;"><tr>
                          <td style="border-radius:10px;background:#1877f2;">
                            <a href="{site}{ctaPath}" style="display:inline-block;padding:14px 26px;color:#ffffff;font-weight:bold;font-size:15px;text-decoration:none;">{ctaText} &rarr;</a>
                          </td>
                        </tr></table>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:18px 32px 26px;border-top:1px solid #e4e6eb;font-size:12px;line-height:1.5;color:#8a8d91;">
                        Recebeste este email porque criaste uma conta em <a href="{site}" style="color:#1877f2;">Kamba Diami</a>.
                        Se não foste tu, ignora esta mensagem ou responde para nos avisares.
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }
}
