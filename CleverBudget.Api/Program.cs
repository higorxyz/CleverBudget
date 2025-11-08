using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AspNetCoreRateLimit;
using CleverBudget.Api.HealthChecks;
using CleverBudget.Api.Swagger;
using CleverBudget.Infrastructure.Notifications;
using CleverBudget.Application.Validators;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Interfaces;
using CleverBudget.Core.Options;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Infrastructure.Services;
using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Serilog;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

QuestPDF.Settings.License = LicenseType.Community;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("🚀 Iniciando CleverBudget API...");

    var builder = WebApplication.CreateBuilder(args);

    if (!builder.Environment.IsProduction())
    {
        var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env");
        if (!File.Exists(envPath))
        {
            envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        }

        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);
            Log.Information("✅ Arquivo .env carregado com sucesso");
            
            // Adicionar variáveis de ambiente ao Configuration
            builder.Configuration.AddEnvironmentVariables();
        }
        else
        {
            Log.Warning($"⚠️ Arquivo .env não encontrado em: {envPath}");
        }
    }

    // Adicionar variáveis de ambiente ao Configuration (produção)
    builder.Configuration.AddEnvironmentVariables();

    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrEmpty(port))
    {
        builder.WebHost.UseUrls($"http://*:{port}");
        Log.Information($"👂 Ouvindo na porta: {port}");
    }

    builder.Host.UseSerilog();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    var railwayConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    if (!string.IsNullOrEmpty(railwayConnectionString))
    {
        databaseUrl = railwayConnectionString;
    }

    if (builder.Environment.IsEnvironment("Test"))
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("CleverBudgetTestDb"));

        Log.Information("🗄️ Banco de dados: InMemory (Testes)");
    }
    else if (builder.Environment.IsProduction())
    {
        Log.Information($"🔍 Ambiente: {builder.Environment.EnvironmentName}");

        string finalConnectionString;

        if (!string.IsNullOrEmpty(databaseUrl) && databaseUrl.StartsWith("postgresql://"))
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            finalConnectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

            Log.Information("🗄️ Banco de dados: PostgreSQL");
        }
        else
        {
            var pgHost = Environment.GetEnvironmentVariable("PGHOST");
            var pgPort = Environment.GetEnvironmentVariable("PGPORT");
            var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
            var pgUser = Environment.GetEnvironmentVariable("PGUSER");
            var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");

            if (!string.IsNullOrEmpty(pgHost) && !string.IsNullOrEmpty(pgDatabase) && !string.IsNullOrEmpty(pgUser) && !string.IsNullOrEmpty(pgPassword))
            {
                finalConnectionString = $"Host={pgHost};Port={pgPort ?? "5432"};Database={pgDatabase};Username={pgUser};Password={pgPassword};SSL Mode=Require;Trust Server Certificate=true";

                Log.Information("🗄️ Banco de dados: PostgreSQL");
            }
            else
            {
                throw new InvalidOperationException("❌ ERRO: PostgreSQL é obrigatório em produção! Configure DATABASE_URL ou as variáveis PGHOST, PGPORT, PGDATABASE, PGUSER, PGPASSWORD no Railway.");
            }
        }

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(finalConnectionString));
    }
    else
    {
        var sqliteConnectionString = connectionString ?? "Data Source=cleverbudget.db";

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(sqliteConnectionString));

        Log.Information("🗄️ Banco de dados: SQLite (Desenvolvimento)");
    }
    
    builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"] ?? Environment.GetEnvironmentVariable("JwtSettings__SecretKey");

    if (string.IsNullOrEmpty(secretKey))
    {
        Log.Warning("⚠️ JWT SecretKey não configurada! Usando chave temporária (NÃO USE EM PRODUÇÃO)");
        secretKey = "ChaveTemporariaParaDesenvolvimento_NaoUseEmProducao_MinimoDe32Caracteres!";
    }

    builder.Services.AddAuthentication(options =>
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
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

    builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
    builder.Services.AddFluentValidationAutoValidation();

    // Configuração do Rate Limiting
    builder.Services.AddMemoryCache();
    if (!builder.Environment.IsEnvironment("Test"))
    {
        builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
        builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
        builder.Services.AddInMemoryRateLimiting();
        builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    }

    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Title = "Erro de validação",
                    Detail = "Um ou mais campos estão inválidos.",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = context.HttpContext.Request.Path
                };

                return new BadRequestObjectResult(problemDetails)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(2, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("x-api-version"),
            new QueryStringApiVersionReader("api-version"));
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.DefaultApiVersion = new ApiVersion(2, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddSwaggerGen();
    builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserProfileService, UserProfileService>();
    builder.Services.AddScoped<IImageUploadService, CloudinaryImageUploadService>();
    builder.Services.AddScoped<ITransactionService, TransactionService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IGoalService, GoalService>();
    builder.Services.AddSingleton<IRealtimeNotifier, NullRealtimeNotifier>();
    builder.Services.Configure<BackupOptions>(builder.Configuration.GetSection(BackupOptions.SectionName));

    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IExportService, ExportService>();
    builder.Services.AddScoped<IExportDeliveryService, ExportDeliveryService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
    builder.Services.AddScoped<IBudgetService, BudgetService>();
    builder.Services.AddScoped<IBackupService, BackupService>();
    builder.Services.AddScoped<IFinancialInsightService, FinancialInsightService>();
    builder.Services.AddHostedService<RecurringTransactionGeneratorService>();
    builder.Services.AddHostedService<BudgetAlertService>();
    builder.Services.AddHostedService<BackupSchedulerService>();

    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
        .AddDbContextCheck<AppDbContext>("database", tags: new[] { "ready" })
        .AddCheck<BackupStorageHealthCheck>("backup_storage", tags: new[] { "ready" });

    var keysPath = Environment.GetEnvironmentVariable("DATAPROTECTION_KEYS_PATH") 
        ?? Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys");
    
    if (!Directory.Exists(keysPath))
    {
        Directory.CreateDirectory(keysPath);
    }

    if (builder.Environment.IsProduction())
    {
        // Em produção, usar proteção sem certificado para evitar problemas de chaves incompatíveis
        // As chaves são protegidas por DPAPI no Windows ou keyring no Linux
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("CleverBudget")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
        
        Log.Information("🔐 Data Protection configurado (Production)");
    }
    else
    {
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("CleverBudget")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
    }

    var app = builder.Build();

    if (!builder.Environment.EnvironmentName.Equals("Design", StringComparison.OrdinalIgnoreCase))
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try
            {
                if (builder.Environment.IsEnvironment("Test"))
                {
                    db.Database.EnsureCreated();
                    Log.Information("✅ Banco de dados em memória inicializado para testes");
                }
                else
                {
                    var canConnect = db.Database.CanConnect();
                    
                    if (!canConnect)
                    {
                        Log.Error("❌ Falha ao conectar ao banco de dados");
                        throw new InvalidOperationException("Não foi possível conectar ao banco de dados");
                    }

                    Log.Information("✅ Conexão com banco estabelecida");

                    if (builder.Environment.IsProduction())
                    {
                        var tableCheckQuery = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'";
                        var hasTables = db.Database.SqlQueryRaw<int>(tableCheckQuery).ToList().FirstOrDefault() > 0;

                        if (hasTables)
                        {
                            // Fix para schema desatualizado - adiciona colunas faltantes na tabela Goals
                            try
                            {
                                Log.Information("🔧 Verificando e corrigindo schema da tabela Goals...");
                                db.Database.ExecuteSqlRaw(@"
                                    DO $$ 
                                    BEGIN 
                                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Goals' AND column_name = 'CategoryId') THEN
                                            ALTER TABLE ""Goals"" ADD COLUMN ""CategoryId"" integer NOT NULL DEFAULT 0;
                                            ALTER TABLE ""Goals"" ADD CONSTRAINT ""FK_Goals_Categories_CategoryId"" FOREIGN KEY (""CategoryId"") REFERENCES ""Categories"" (""Id"") ON DELETE RESTRICT;
                                            CREATE INDEX ""IX_Goals_CategoryId"" ON ""Goals"" (""CategoryId"");
                                        END IF;
                                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Goals' AND column_name = 'Month') THEN
                                            ALTER TABLE ""Goals"" ADD COLUMN ""Month"" integer NOT NULL DEFAULT 1;
                                        END IF;
                                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Goals' AND column_name = 'Year') THEN
                                            ALTER TABLE ""Goals"" ADD COLUMN ""Year"" integer NOT NULL DEFAULT 2025;
                                        END IF;
                                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Goals' AND column_name = 'CreatedAt') THEN
                                            ALTER TABLE ""Goals"" ADD COLUMN ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW();
                                        END IF;
                                    END $$;
                                ");
                                Log.Information("✅ Schema da tabela Goals verificado/corrigido");
                            }
                            catch (Exception fixEx)
                            {
                                Log.Warning(fixEx, "⚠️ Erro ao corrigir schema: {Message}", fixEx.Message);
                            }

                            var pendingMigrations = db.Database.GetPendingMigrations().ToList();

                            if (pendingMigrations.Any())
                            {
                                Log.Information($"🔄 Aplicando {pendingMigrations.Count} migration(s) pendente(s)...");
                                try
                                {
                                    db.Database.Migrate();
                                    Log.Information("✅ Migrations aplicadas com sucesso");
                                }
                                catch (Exception migrateEx)
                                {
                                    Log.Warning(migrateEx, "⚠️ Erro ao aplicar migrations: {Message}", migrateEx.Message);
                                }
                            }
                            else
                            {
                                Log.Information("✅ Banco de dados atualizado");
                            }
                        }
                        else
                        {
                            Log.Information("🆕 Criando estrutura do banco de dados...");
                            try
                            {
                                db.Database.Migrate();
                                Log.Information("✅ Banco de dados criado com sucesso");
                            }
                            catch (Exception migrateEx)
                            {
                                Log.Error(migrateEx, "❌ Erro ao criar banco de dados: {Message}", migrateEx.Message);
                                throw;
                            }
                        }
                    }
                    else
                    {
                        // Development - apenas aplica migrations sem logs verbosos
                        db.Database.Migrate();
                        Log.Information("✅ Banco de dados atualizado");
                    }
                }
            }
            catch (Exception ex) when (!ex.Message.Contains("Não foi possível conectar"))
            {
                Log.Error(ex, "❌ Erro ao inicializar banco de dados: {Message}", ex.Message);
                throw;
            }
        }
    }

    app.UseSwagger();

    var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwaggerUI(options =>
    {
        foreach (var description in apiVersionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"CleverBudget API {description.GroupName.ToUpperInvariant()}");
        }

        options.RoutePrefix = string.Empty;
        options.DocumentTitle = "CleverBudget API Documentation";
        options.DefaultModelsExpandDepth(-1); // Oculta schemas por padrão
    });

    if (!app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors("AllowAll");
    app.UseSerilogRequestLogging();

    // Rate Limiting Middleware
    if (!app.Environment.IsEnvironment("Test"))
    {
        app.UseIpRateLimiting();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live"),
        ResponseWriter = WriteHealthResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
        ResponseWriter = WriteHealthResponse
    });

    Log.Information("✅ CleverBudget API pronta!");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Erro fatal ao iniciar a aplicação");
}
finally
{
    Log.CloseAndFlush();
}

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";

    var response = new
    {
        status = report.Status.ToString(),
        totalDurationMilliseconds = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            durationMilliseconds = entry.Value.Duration.TotalMilliseconds,
            data = entry.Value.Data?.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString())
        })
    };

    var json = JsonSerializer.Serialize(response);
    return context.Response.WriteAsync(json);
}

// Torna a classe Program acessível para testes
public partial class Program { }