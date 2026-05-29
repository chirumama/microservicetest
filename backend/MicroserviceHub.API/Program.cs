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
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

Console.WriteLine("APP STARTING...");
var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:4173",
                "http://192.168.17.129:30080",
                "http://3.110.46.238:3001", 
                "http://3.110.46.238:30082",
                "http://3.110.46.238:30081",
                "http://65.2.6.12:5080",
                "http://3.110.46.238:9000",
                "http://3.110.46.238:9180",
                "http://3.110.46.238:9080"
                 )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Register singletons FIRST so we can resolve for JWT config's
builder.Services.AddSingleton<RsaKeyProvider>();
builder.Services.AddSingleton<OAuthTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<RsaKeyProvider>((options, keys) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer           = true,
    ValidIssuer              = builder.Configuration["OAuth:Issuer"],
    ValidateAudience         = true,
    ValidAudience            = builder.Configuration["OAuth:DashboardAudience"],
    ValidateLifetime         = false,   // ← allow tokens without expiry for dashboard tokens too
    ValidateIssuerSigningKey = true,
    IssuerSigningKey         = keys.GetPublicKey(),
    ClockSkew                = TimeSpan.Zero
};

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode  = 401;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync("{\"error\":\"Unauthorized — valid dashboard token required\"}");
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<ApisixService>();
builder.Services.AddScoped<ApisixService>();

builder.Services.AddScoped<IAuthService,           AuthService>();
builder.Services.AddScoped<IAuthRepository,        AuthRepository>();
builder.Services.AddScoped<IApplicationService,    ApplicationService>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IOAuthService,          OAuthService>();
builder.Services.AddHttpClient("apisix-admin");
builder.Services.AddScoped<IRouteSyncService, RouteSyncService>();
builder.Services.AddHostedService<RouteSyncBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MicroserviceHub.API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Paste the access_token from POST /v1.0.1/auth/login"
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
builder.Services.AddHealthChecks()
    // PostgreSQL health check using NpgSql
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgres")
    .AddCheck("self", () => HealthCheckResult.Healthy());

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
app.MapHealthChecks("/health/live",     new HealthCheckOptions { Predicate = c => c.Name == "self" }); app.MapHealthChecks("/health/ready",     new HealthCheckOptions { Predicate = _ => true });

app.MapControllers();
app.Run();
