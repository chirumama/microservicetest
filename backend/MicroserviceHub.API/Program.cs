using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Application.Services;
using MicroserviceHub.API.Infrastructure.Repositories;
using MicroserviceHub.API.Infrastructure.ExternalServices;
using MicroserviceHub.API.Middleware;
using MicroserviceHub.API.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Console.WriteLine("APP STARTING...");
var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System",    Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("LogType") && e.Properties["LogType"].ToString() == "\"Request\"")
            .WriteTo.File("logs/request-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30))
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("LogType") && e.Properties["LogType"].ToString() == "\"Response\"")
            .WriteTo.File("logs/response-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30))
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(e => e.Level >= Serilog.Events.LogEventLevel.Error)
            .WriteTo.File("logs/error-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30))
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(e => !e.Properties.ContainsKey("LogType") && e.Level < Serilog.Events.LogEventLevel.Error)
            .WriteTo.File("logs/application-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:4173",
                "http://192.168.17.129:30080",
                "http://192.168.17.129:32417")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ── RSA key provider (singleton — loaded once at startup) ─────────────────────
builder.Services.AddSingleton<RsaKeyProvider>();
builder.Services.AddSingleton<OAuthTokenService>();

var rsaProvider = new RsaKeyProvider(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["OAuth:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["OAuth:DashboardAudience"],

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaProvider.GetPrivateKey(),

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
// ── APISix ────────────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<ApisixService>();
builder.Services.AddScoped<ApisixService>();

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,        AuthService>();
builder.Services.AddScoped<IAuthRepository,     AuthRepository>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IOAuthService,       OAuthService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MicroserviceHub.API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Enter the dashboard JWT token from POST /auth/login"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

Console.WriteLine("APP RUNNING...");

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null) Log.Error(error.Error, "Unhandled Exception");

        var statusCode = error?.Error switch
        {
            UnauthorizedAccessException => 401,
            InvalidOperationException   => 400,
            KeyNotFoundException        => 404,
            _                           => 500
        };

        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/json";
        var message = error?.Error?.Message ?? "Internal Server Error";
        await context.Response.WriteAsync($"{{\"error\":\"{message}\"}}");
    });
});

app.MapControllers();
app.Run();