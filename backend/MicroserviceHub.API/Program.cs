using MicroserviceHub.API.Application.Interfaces;
using MicroserviceHub.API.Infrastructure.ExternalServices;
using MicroserviceHub.API.Application.Services;
using MicroserviceHub.API.Infrastructure.Repositories;
using MicroserviceHub.API.Middleware;
using MicroserviceHub.API.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog; 
using System.Text;
Console.WriteLine("APP STARTING...");
var builder = WebApplication.CreateBuilder(args);

// Replace with this:
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)

        // Console — everything
        .WriteTo.Console(
            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        )

        // request-.log — only REQUEST logs (written by middleware)
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(e =>
                e.Properties.ContainsKey("LogType") &&
                e.Properties["LogType"].ToString() == "\"Request\"")
            .WriteTo.File(
                path: "logs/request-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
        )

        // response-.log — only RESPONSE logs (written by middleware)
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(e =>
                e.Properties.ContainsKey("LogType") &&
                e.Properties["LogType"].ToString() == "\"Response\"")
            .WriteTo.File(
                path: "logs/response-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
        )

        // error-.log — only Error level and above
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(e =>
                e.Level >= Serilog.Events.LogEventLevel.Error)
            .WriteTo.File(
                path: "logs/error-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
        )

        // application-log — everything that is NOT request/response/errors
        // i.e. your Log.Information(...) calls from services
        .WriteTo.Logger(lc => lc
            .Filter.ByIncludingOnly(e =>
                !e.Properties.ContainsKey("LogType") &&
                e.Level < Serilog.Events.LogEventLevel.Error)
            .WriteTo.File(
                path: "logs/application-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
        );
});

// CORS — allow Vite dev server and preview
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:4173",
                "http://192.168.17.129:30080",
                "http://192.168.17.129:32417"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "MicroserviceHub.API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddScoped<IAuthService, MicroserviceHub.API.Application.Services.AuthService>();
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddHttpClient<ApisixService>();
builder.Services.AddScoped<ApisixService>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };

    // Return 401 JSON instead of redirect
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async ctx =>
        {
            ctx.HandleResponse();
            ctx.Response.StatusCode  = 401;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"error\":\"Unauthorized\"}");
        }
    };
});

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
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
// Global exception handler — returns JSON so the frontend can read the message
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>();

        if (error != null)
        {
            Log.Error(error.Error, "Unhandled Exception");
        }

        var statusCode = error?.Error switch
        {
            UnauthorizedAccessException => 401,
            KeyNotFoundException        => 404,
            _                           => 500
        };

        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/json";

        var message = error?.Error?.Message ?? "Internal Server Error";
        await context.Response.WriteAsync($"{{\"error\":\"{message}\"}}");
    });
});

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
