using CleverBudget.Core.Entities;
using CleverBudget.Infrastructure.Data;
using CleverBudget.Core.Interfaces;
using CleverBudget.Infrastructure.Services;
using CleverBudget.Application.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using QuestPDF.Infrastructure;
using AspNetCoreRateLimit;

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
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .SelectMany(e => e.Value!.Errors.Select(x => x.ErrorMessage))
                    .ToList();

                return new BadRequestObjectResult(new
                {
                    message = "Erro de validação",
                    errors = errors
                });
            };
        });

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "CleverBudget API",
            Version = "v1",
            Description = "API para controle financeiro inteligente",
            Contact = new OpenApiContact
            {
                Name = "CleverBudget API"
            }
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Insira o token JWT no formato: Bearer {seu token}"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });
    });

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
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IExportService, ExportService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
    builder.Services.AddScoped<IBudgetService, BudgetService>();
    builder.Services.AddHostedService<RecurringTransactionGeneratorService>();
    builder.Services.AddHostedService<BudgetAlertService>();

    var keysPath = Environment.GetEnvironmentVariable("DATAPROTECTION_KEYS_PATH") 
        ?? Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys");
    
    if (!Directory.Exists(keysPath))
    {
        Directory.CreateDirectory(keysPath);
    }

    if (builder.Environment.IsProduction())
    {
        var rsa = RSA.Create(2048);
        var certRequest = new CertificateRequest("CN=CleverBudget", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));

        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .ProtectKeysWithCertificate(cert);
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
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CleverBudget API v1");
        c.RoutePrefix = string.Empty;
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

// Torna a classe Program acessível para testes
public partial class Program { }