using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace GymApi.Api.Infrastructure.Swagger;

// ReSharper disable once ClassNeverInstantiated.Global
public class RequiredSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null)
        {
            return;
        }

        // Handle properties from the type itself
        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // ReSharper disable once InvertIf
            if (property.GetCustomAttribute<RequiredAttribute>() != null)
            {
                var schemaProperty = schema.Properties.FirstOrDefault(p => p.Key.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
                // ReSharper disable once InvertIf
                if (schemaProperty.Key != null)
                {
                    schemaProperty.Value.Nullable = false;
                    schema.Required ??= new HashSet<string>();
                    schema.Required.Add(schemaProperty.Key);
                }
            }
        }

        // Handle properties from record primary constructor parameters (if applicable)
        // This is a more robust way to handle [Required] on record properties
        foreach (var parameter in context.Type.GetConstructors()
                     .SelectMany(c => c.GetParameters())
                     .Where(p => p.GetCustomAttribute<RequiredAttribute>() != null))
        {
            var schemaProperty = schema.Properties.FirstOrDefault(p => p.Key.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (schemaProperty.Key != null)
            {
                schemaProperty.Value.Nullable = false;
                schema.Required ??= new HashSet<string>();
                schema.Required.Add(schemaProperty.Key);
            }
        }
    }
}
