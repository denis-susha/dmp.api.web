using System.Security.Claims;
using System.Text;
using DMP.BL.Factories;
using DMP.BL.Models.AppSettings;
using DMP.BL.Models.Auth;
using DMP.BL.Services;
using DMP.BL.Services.User;
using DMP.Crosscutting.Models;
using DMP.DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using StackExchange.Redis;

namespace API.Web;

public static class ServiceExtensions
{
    public static IServiceCollection AddDmpServices(this IServiceCollection services)
    {
        services.AddTransient<IMenuService, MenuService>();
        services.AddTransient<IMenuFactory, MenuFactory>();
        services.AddTransient<ITranslationService, TranslationService>();
        services.AddTransient<IRedisService, RedisService>();
        services.AddTransient<IProductService, ProductService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddTransient<IMailService, MailService>();
        services.AddTransient<IRegistrationService, RegistrationService>();
        services.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IOrderService, OrderService>();
        services.AddTransient<ICartService, CartService>();
        services.AddTransient<IUserService, UserService>();
        services.AddTransient<IProductImagesService, ProductImagesService>();
        services.AddSingleton<UserConnectionService>();
        services.AddTransient<IProductManagerService, ProductManagerService>();
        services.AddTransient<IProductFileStorageService, ProductFileStorageService>();
        services.AddTransient<IAdminService, AdminService>();
        services.AddTransient<IDownloadOrderService, DownloadOrderService>();
        services.AddTransient<ICacheManagerService, CacheManagerService>();
        services.AddTransient<ISupportService, SupportService>();
        services.AddTransient<IFinancesService, FinancesService>();
        services.AddTransient<ISalesService, SalesService>();
        services.AddTransient<IStoreService, StoreService>();
        services.AddTransient<ISellerService, SellerService>();
        services.AddTransient<IPageService, PageService>();
        services.AddTransient<IWithdrawalService, WithdrawalService>();
        services.AddTransient<IBlogService, BlogService>();

        return services;
    }

    public static IServiceCollection AddDmpDbContexts(this IServiceCollection services, string? dmpConnectionString,
        string? billingConnectionString)
    {
        services.AddDbContextFactory<DmpDbContext>(options => options.UseNpgsql(BuildDataSource(dmpConnectionString)));
        services.AddDbContextFactory<BillingDbContext>(options =>
            options.UseNpgsql(BuildDataSource(billingConnectionString)));

        return services;
    }

    private static NpgsqlDataSource BuildDataSource(string? connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        // Required to map POCOs/collections to jsonb columns.
        dataSourceBuilder.EnableDynamicJson();

        return dataSourceBuilder.Build();
    }

    public static IServiceCollection AddDmpRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisSettings>(configuration.GetSection("Redis"))
            .AddSingleton(sp => sp.GetRequiredService<IOptions<RedisSettings>>().Value);

        // Connection parameters come from the REDIS_HOST / REDIS_PORT / REDIS_PASSWORD environment variables
        // shared with the other DMP services.
        var redisConnectionString =
            $"{configuration["REDIS_HOST"]}:{configuration["REDIS_PORT"]},password={configuration["REDIS_PASSWORD"]},abortConnect=False";

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

        return services;
    }

    public static IServiceCollection AddDmpMinio(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MinioSettings>(configuration.GetSection("Minio"));
        services.AddSingleton<IMinioClientFactory>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MinioSettings>>().Value;
            return new MinioClientFactory(settings.Endpoint, settings.AccessKey, settings.SecretKey);
        });

        return services;
    }

    public static IServiceCollection AddDmpAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettingsSection = configuration.GetSection("JwtSettings");
        services.Configure<JwtSettings>(jwtSettingsSection);
        var jwtSettings = jwtSettingsSection.Get<JwtSettings>()
                          ?? throw new InvalidOperationException("JwtSettings section is not configured.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // The frontends keep the access token in an HttpOnly cookie instead of the Authorization header.
                        var accessToken = context.Request.Cookies["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("RequireIsSeller", policy => policy.RequireClaim("isSeller", "true"))
            .AddPolicy("RequireAdmin", policy => policy.RequireClaim(ClaimTypes.Role, "admin"));

        return services;
    }
}
