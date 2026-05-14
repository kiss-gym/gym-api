using Npgsql;
using System.Text.Json.Serialization;
using GymApi.Api.Infrastructure.Middleware;
using GymApi.Api.Infrastructure.Swagger;
using GymApi.Application.SessionTracking;
using GymApi.Application.UserManagement;
using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using GymApi.Infrastructure;
using GymApi.Infrastructure.Environment;
using GymApi.Infrastructure.SessionTracking;
using GymApi.Infrastructure.UserManagement;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
var npgsqlDataSource = new NpgsqlDataSourceBuilder(builder.Configuration["Supabase:ConnectionString"])
    .EnableDynamicJson()
    .Build();
builder.Services.AddDbContext<GymApiDbContext>(options =>
    options.UseNpgsql(npgsqlDataSource));

// ── Authentication ──────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Supabase exposes JWKS at {Url}/auth/v1/.well-known/jwks.json
        // Authority triggers automatic JWKS discovery — no secret needed
        options.Authority = $"{builder.Configuration["Supabase:Url"]}/auth/v1";
        options.Audience = "authenticated";
    });

builder.Services.AddAuthorization();

// ── Controllers ─────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Kiss Gym API",
        Version = "v1",
        Description = "API for Kiss Gym - Session Tracking and User Management."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.SchemaFilter<RequiredSchemaFilter>();

    c.TagActionsBy(api =>
    {
        if (api.GroupName != null)
        {
            return [api.GroupName];
        }

        var controllerName = api.ActionDescriptor.RouteValues["controller"];
        return controllerName switch
        {
            "SessionTracking" => ["Sessions"],
            "User" => ["Users"],
            _ => ["GymApi"]
        };
    });

    c.DocInclusionPredicate((_, _) => true);
});

// ── User Management ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddScoped<IUserContext, MockUserContext>();

// ── Session Tracking ────────────────────────────────────────────────────────
builder.Services.AddSingleton<ITrainingSessionRepository, InMemoryTrainingSessionRepository>();
builder.Services.AddScoped<ITrainingSessionService, TrainingSessionService>();

// ── Infrastructure ──────────────────────────────────────────────────────────
builder.Services.AddSingleton(new VersionProvider(VersionProvider.ReadVersionFromAssembly(),
    VersionProvider.GetRuntimeDescription()));

builder.Services.AddTransient<ExceptionMiddleware>();
builder.Services.AddSingleton<RequestResponseLoggingMiddleware>();

var app = builder.Build();

app.UseSwagger(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
});
app.UseSwaggerUI();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// ── Auth middleware must come before MapControllers ─────────────────────────
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", (HttpContext context, VersionProvider versionProvider) =>
{
    var info = new
    {
        Name = "Kiss Gym API",
        versionProvider.CodeVersion,
        versionProvider.LastCommitDate,
        Environment = app.Environment.EnvironmentName,
        Docs = "/swagger"
    };

    if (context.Request.Headers.Accept.Any(h => h != null && h.Contains("text/html")))
    {
        return Results.Content(
            $"<html><body style='font-family: sans-serif; padding: 2rem;'>" +
            $"<h1>{info.Name}</h1>" +
            $"<p><strong>Version:</strong> {info.CodeVersion}</p>" +
            $"<p><strong>Last Commit Date:</strong> {info.LastCommitDate}</p>" +
            $"<p><strong>Environment:</strong> {info.Environment}</p>" +
            $"<hr/>" +
            $"<p><a href='/swagger'>Go to API Documentation (Swagger)</a></p>" +
            $"</body></html>", "text/html");
    }

    return Results.Ok(info);
});

app.MapControllers();
app.Run();
