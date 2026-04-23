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
                "http://192.168.17.129:32417")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Register singletons FIRST so we can resolve for JWT config
builder.Services.AddSingleton<RsaKeyProvider>();
builder.Services.AddSingleton<OAuthTokenService>();

// Build a temp provider to get the public key for JWT validation setup
// This is the correct pattern — resolves from the same DI container
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Resolve from the container — same singleton the rest of the app uses
        var sp         = builder.Services.BuildServiceProvider();
        var keys       = sp.GetRequiredService<RsaKeyProvider>();
        var config     = builder.Configuration;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = config["OAuth:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = config["OAuth:DashboardAudience"],
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = keys.GetPublicKey(),  // public key only
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