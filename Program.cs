using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using LiteFactoryWeb.Services;

var builder = WebApplication.CreateBuilder(args);
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

builder.Services.AddRazorPages();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "LiteFactoryWeb.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddHttpClient<LiteFactoryApiClient>(client =>
{
    client.BaseAddress = ResolveLiteFactoryApiBaseUri(builder.Configuration);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static Uri ResolveLiteFactoryApiBaseUri(IConfiguration configuration)
{
    var environmentBaseUrl = Environment.GetEnvironmentVariable("LITEFACTORY_API_BASE_URL");
    var configuredBaseUrl = !string.IsNullOrWhiteSpace(environmentBaseUrl)
        ? environmentBaseUrl
        : configuration["LiteFactoryApi:BaseUrl"] ?? "http://localhost:5011";

    if (!Uri.TryCreate(EnsureTrailingSlash(configuredBaseUrl.Trim()), UriKind.Absolute, out var baseUri) ||
        (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException("LiteFactory API base URL must be an absolute HTTP or HTTPS URL.");
    }

    return baseUri;
}

static string EnsureTrailingSlash(string value)
{
    return value.EndsWith("/", StringComparison.Ordinal) ? value : $"{value}/";
}
