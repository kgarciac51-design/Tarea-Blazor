using System.Security.Cryptography;
using WebEmpresa.Components;

var builder = WebApplication.CreateBuilder(args);

// [Seguridad] Oculta la cabecera "Server: Kestrel" para no revelar la tecnología del servidor.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// [ZAP] Cookie Sin Flag de Seguridad:
// la cookie antiforgery se marca como Secure cuando la petición llega por HTTPS.
// (Se usa SameAsRequest y no Always porque Always lanza una excepción si se abre por http://)
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// [ZAP] Strict-Transport-Security: HSTS de 1 año para el ambiente de producción.
// ASP.NET Core no envía HSTS a "localhost" a propósito (ver tabla comparativa).
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

// HttpClient para consumir ApiEmpresa
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7043/")
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// [ZAP] Cabeceras de seguridad en todas las respuestas.
app.Use(async (context, next) =>
{
    // Si la petición se re-ejecuta (página de error), se conserva el nonce original.
    if (context.Items.ContainsKey("csp-nonce"))
    {
        await next();
        return;
    }

    // Nonce aleatorio por petición: permite SOLO el <script type="importmap"> que genera Blazor.
    var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    context.Items["csp-nonce"] = nonce;

    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;

        // CSP: Directiva Wildcard / Failure to Define Directive with No Fallback /
        //      script-src unsafe-inline / style-src unsafe-inline
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            $"script-src 'self' 'nonce-{nonce}'; " +
            "style-src 'self'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-src 'none'; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'none'";

        // Falta encabezado X-Content-Type-Options
        headers["X-Content-Type-Options"] = "nosniff";

        // Protección adicional contra clickjacking y fuga de información
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // Reexaminar las Directivas de Control de Caché:
        // las páginas muestran datos personales (CUI, NIT, teléfono), no deben guardarse en caché.
        if (context.Response.ContentType?.StartsWith("text/html") == true)
        {
            headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            headers["Pragma"] = "no-cache";
        }

        return Task.CompletedTask;
    });

    await next();
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    // Blazor agrega por defecto una segunda cabecera CSP solo con "frame-ancestors 'self'".
    // Se desactiva porque la CSP completa de arriba ya incluye frame-ancestors 'none' (más estricto).
    .AddInteractiveServerRenderMode(options => options.ContentSecurityFrameAncestorsPolicy = null);

app.Run();