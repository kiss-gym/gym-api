using GymApi.Api.Infrastructure;
using GymApi.Api.Infrastructure.Middleware;
using GymApi.Api.Infrastructure.Swagger;
using GymApi.Application.SessionTracking;
using GymApi.Application.UserManagement;
using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using GymApi.Infrastructure.Environment;
using GymApi.Infrastructure.SessionTracking;
using GymApi.Infrastructure.UserManagement;
using Microsoft.OpenApi.Models;
using Orleans.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure Orleans Silo
builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("sessionStore");
});

builder.Services.AddControllers();
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
            "SessionTracking" => ["Sessions" ],
            "User" => [ "Users" ],
            _ => ["GymApi"]
        };
    });

    c.DocInclusionPredicate((_, _) => true);
});

// User Management
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddScoped<IUserContext, MockUserContext>();
builder.Services.AddSingleton<IActiveUserProvider, OrleansActiveUserProvider>();

// Session Tracking
builder.Services.AddScoped<ITrainingSessionLifecycleService, TrainingSessionLifecycleService>();
builder.Services.AddSingleton<ITrainingSessionLifecycleProvider, OrleansTrainingSessionLifecycleProvider>();

// Environment (supporting subdomain)
builder.Services.AddSingleton(new VersionProvider(VersionProvider.ReadVersionFromAssembly(), VersionProvider.GetRuntimeDescription()));

builder.Services.AddTransient<ExceptionMiddleware>();

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionMiddleware>();

app.UseMiddleware<RequestResponseLoggingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

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
