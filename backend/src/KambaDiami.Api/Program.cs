using System.Text.Json.Serialization;
using KambaDiami.Api.Hubs;
using KambaDiami.Api.Services;
using KambaDiami.Infrastructure;
using KambaDiami.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// O segredo de desenvolvimento está em appsettings.Development.json e é público no repositório.
// Em produção tem de vir de uma variável de ambiente (Jwt__Secret).
const string DevelopmentJwtSecret = "kamba-diami-dev-super-secret-key-change-in-production-2026";
if (!builder.Environment.IsDevelopment() && builder.Configuration["Jwt:Secret"] == DevelopmentJwtSecret)
{
    throw new InvalidOperationException("Em produção, define Jwt:Secret com um valor próprio e secreto.");
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<PasswordResetSender>();
builder.Services.AddScoped<MunicipalityAreaResolver>();
builder.Services.AddKambaDiamiRateLimits();

// Atrás de um proxy (Nginx/IIS), o IP real do cliente chega neste cabeçalho; é usado no
// registo de logins e nos limites de tentativas.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Por omissão só se confia em proxies em localhost. Num contentor (Railway) o proxy tem outro
    // endereço, e sem isto todos os pedidos pareceriam vir do mesmo IP e partilhariam os limites de tentativas.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads", "photos"));
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data", "private", "contracts", "generated"));
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data", "private", "contracts", "signed"));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Kamba Diami API v1");
        options.RoutePrefix = "swagger";
    });
}

// Os dados de referência (papéis, províncias, categorias e o primeiro administrador) são carregados
// em qualquer ambiente e não são criados utilizadores de teste. As migrações podem ser desligadas
// (Database:ApplyMigrationsOnStartup = false) quando são aplicadas por script com uma conta própria,
// para que o utilizador da aplicação fique só com permissões de leitura e escrita.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KambaDiamiDbContext>();
    if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
    {
        await db.Database.MigrateAsync();
    }
    await DbSeeder.SeedAsync(scope.ServiceProvider);
    await db.JobPostings.AsNoTracking().Take(1).ToListAsync();
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

var hashedBuildFile = new System.Text.RegularExpressions.Regex(
    @"-[A-Z0-9]{8}\.(js|css)$", System.Text.RegularExpressions.RegexOptions.Compiled);

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    // O index.html aponta para os ficheiros JS com hash de cada compilação. Se o navegador o guardar em
    // cache, depois de um deploy continua a pedir ficheiros que já não existem e a página deixa de funcionar.
    // Os ficheiros JS/CSS têm um hash no nome (ex.: main-YWNEWYWH.js), por isso nunca mudam: o navegador pode
    // guardá-los durante um ano e não volta a pedi-los ao servidor, que está longe (EUA) e cada pedido custa ~0,4 s.
    // Logótipos e fotografias ficam um dia em cache.
    OnPrepareResponse = ctx =>
    {
        var name = ctx.File.Name;
        var headers = ctx.Context.Response.Headers;
        if (name.Equals("index.html", StringComparison.OrdinalIgnoreCase))
            headers.CacheControl = "no-cache, no-store, must-revalidate";
        else if (hashedBuildFile.IsMatch(name))
            headers.CacheControl = "public, max-age=31536000, immutable";
        else if (ctx.Context.Request.Path.StartsWithSegments("/assets") || name.Equals("favicon.ico", StringComparison.OrdinalIgnoreCase))
            headers.CacheControl = "public, max-age=86400";
    }
});

app.UseCors("AllowAngularDev");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

// Verificação de saúde usada pelo Railway: a API responde e consegue falar com a base de dados.
app.MapGet("/health", async (KambaDiamiDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ok" }) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

// O Angular compilado fica em wwwroot e é servido pelo mesmo domínio da API (Capítulo VI), por isso
// o navegador não precisa de CORS. Qualquer rota sem ponto e fora de /api, /hubs e /uploads devolve
// o index.html, para a navegação interna funcionar ao actualizar a página ou abrir uma ligação directa.
app.MapFallbackToFile("{*path:regex(^(?!(api|hubs|uploads)(/|$))[^.]*$)}", "index.html", new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate"
});

app.Run();
