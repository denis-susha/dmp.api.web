using System.Text.Json.Serialization;
using API.Web;
using API.Web.Hubs;
using API.Web.OpenApi;
using DMP.BL.Models.AppSettings;
using DMP.BL.Models.Registration;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.UseUtcTimestamp = true;
});

var configuration = builder.Configuration;
var isDevelopment = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Local");

builder.Services.AddDmpDbContexts(
    configuration.GetConnectionString("DmpConnection"),
    configuration.GetConnectionString("BillingDbConnection"));
builder.Services.AddDmpRedis(configuration);
builder.Services.AddDmpMinio(configuration);
builder.Services.AddDmpAuthentication(configuration);

builder.Services.Configure<JwtRegistrationSettings>(configuration.GetSection("JwtRegistrationSettings"));
builder.Services.Configure<CfSettings>(configuration.GetSection("CfSettings"));
builder.Services.Configure<DmpHostsSettings>(configuration.GetSection("DmpHosts"));
builder.Services.Configure<BasicAuthorizeOptions>(configuration.GetSection(BasicAuthorizeOptions.Position));

builder.Services.AddHttpClient("BitcartBackend", client =>
    client.BaseAddress = new Uri(configuration["BitcartBackend:Url"]
                                 ?? throw new InvalidOperationException("BitcartBackend:Url is not configured.")));

builder.Services.AddDmpServices();

var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

builder.Services.AddOpenApi(options => options.AddOperationTransformer<HideParameterTransformer>());

builder.Services.AddSignalR(options => options.EnableDetailedErrors = isDevelopment);

var app = builder.Build();

if (isDevelopment)
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("AllowSpecificOrigin");

// HTTPS redirection is intentionally disabled: TLS is terminated by the reverse proxy in front of the API.

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHub<PaymentHub>("/paymenthub");

app.Run();
