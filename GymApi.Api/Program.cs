using GymApi.Api.Infrastructure;
using GymApi.Api.Infrastructure.Swagger;
using GymApi.Application.SessionTracking;
using GymApi.Application.UserManagement;
using GymApi.Domain.SessionTracking;
using GymApi.Domain.UserManagement;
using GymApi.Infrastructure.SessionTracking;
using GymApi.Infrastructure.UserManagement;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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
            return new[] { api.GroupName };
        }

        var controllerName = api.ActionDescriptor.RouteValues["controller"];
        return controllerName switch
        {
            "SessionTracking" => new[] { "Sessions" },
            "User" => new[] { "Users" },
            _ => new[] { controllerName ?? "Default" }
        };
    });

    c.DocInclusionPredicate((_, _) => true);
});

// User Management (generic subdomain)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddScoped<IUserContext, MockUserContext>();

// Session Tracking (core subdomain)
builder.Services.AddScoped<ICurrentSessionService, CurrentSessionService>();
builder.Services.AddSingleton<ISessionRepository, InMemorySessionRepository>();

builder.Services.AddTransient<ExceptionMiddleware>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();
