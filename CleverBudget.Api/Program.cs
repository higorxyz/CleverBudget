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

    var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
    if (!File.Exists(envPath))
    {
        envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env");
    }

    if (File.Exists(envPath))
    {
        DotNetEnv.Env.Load(envPath);
        Log.Information("✅ Arquivo .env carregado com sucesso");
    }
    else
    {
        Log.Warning($"⚠️ Arquivo .env não encontrado em: {envPath}");
    }

    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddEnvironmentVariables();

    if (builder.Environment.IsDevelopment())
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_SECRET_KEY")))
        {
            Log.Warning("⚠️ JWT_SECRET_KEY não definida! Configure no arquivo .env");
        }
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BREVO_API_KEY")))
        {
            Log.Warning("⚠️ BREVO_API_KEY não definida! Configure no arquivo .env");
        }
    }

    builder.Host.UseSerilog();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    if (builder.Environment.IsProduction() || !string.IsNullOrEmpty(databaseUrl))
    {
        Log.Information($"🔍 Ambiente: {builder.Environment.EnvironmentName}");
        Log.Information($"🔍 DATABASE_URL presente: {!string.IsNullOrEmpty(databaseUrl)}");

        string finalConnectionString;

        if (!string.IsNullOrEmpty(databaseUrl) && databaseUrl.StartsWith("postgresql://"))
        {
            // Usa DATABASE_URL se disponível
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            finalConnectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

            Log.Information("🗄️ Usando PostgreSQL via DATABASE_URL");
            Log.Information($"🔍 Connection string: Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')}");
        }
        else
        {
            // Tenta construir connection string a partir de variáveis individuais do Railway
            var pgHost = Environment.GetEnvironmentVariable("PGHOST");
            var pgPort = Environment.GetEnvironmentVariable("PGPORT");
            var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");
            var pgUser = Environment.GetEnvironmentVariable("PGUSER");
            var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");

            Log.Information($"🔍 PGHOST: {pgHost}");
            Log.Information($"🔍 PGPORT: {pgPort}");
            Log.Information($"🔍 PGDATABASE: {pgDatabase}");
            Log.Information($"🔍 PGUSER: {pgUser}");
            Log.Information($"🔍 PGPASSWORD presente: {!string.IsNullOrEmpty(pgPassword)}");

            if (!string.IsNullOrEmpty(pgHost) && !string.IsNullOrEmpty(pgDatabase) && !string.IsNullOrEmpty(pgUser) && !string.IsNullOrEmpty(pgPassword))
            {
                finalConnectionString = $"Host={pgHost};Port={pgPort ?? "5432"};Database={pgDatabase};Username={pgUser};Password={pgPassword};SSL Mode=Require;Trust Server Certificate=true";

                Log.Information("🗄️ Usando PostgreSQL via variáveis individuais");
                Log.Information($"🔍 Connection string: Host={pgHost};Port={pgPort ?? "5432"};Database={pgDatabase}");
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

        Log.Information("🗄️ Usando SQLite (Desenvolvimento)");
        Log.Information($"🔍 Connection string: {sqliteConnectionString}");
    }    builder.Services.AddIdentity<User, IdentityRole>(options =>
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
    var secretKey = jwtSettings["SecretKey"] ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

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
    builder.Services.AddScoped<ITransactionService, TransactionService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IGoalService, GoalService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IExportService, ExportService>();
    builder.Services.AddScoped<IEmailService, EmailService>();

    var keysPath = Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys");
    
    if (!Directory.Exists(keysPath))
    {
        Directory.CreateDirectory(keysPath);
    }

    Log.Information($"🔑 Data Protection Keys Path: {keysPath}");

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
        
        Log.Information("⚙️ Data Protection configurado para desenvolvimento (chaves não criptografadas)");
    }

    var app = builder.Build();

    if (!builder.Environment.EnvironmentName.Equals("Design", StringComparison.OrdinalIgnoreCase))
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try
            {
                // Verifica se consegue conectar ao banco
                var canConnect = db.Database.CanConnect();
                Log.Information($"🔍 Conexão com banco: {(canConnect ? "OK" : "FALHA")}");

                if (canConnect)
                {
                    // Tenta aplicar migrations apenas se necessário
                    var pendingMigrations = db.Database.GetPendingMigrations().ToList();
                    Log.Information($"🔍 Migrations pendentes: {pendingMigrations.Count}");

                    if (pendingMigrations.Any())
                    {
                        db.Database.Migrate();
                        Log.Information("✅ Migrations aplicadas com sucesso!");
                    }
                    else
                    {
                        Log.Information("✅ Banco de dados já está atualizado!");
                    }
                }
                else
                {
                    Log.Warning("⚠️ Não foi possível conectar ao banco. Pulando migrations.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "⚠️ Erro ao verificar/aplicar migrations. Continuando sem migrations.");
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

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("✅ CleverBudget API iniciada com sucesso!");

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