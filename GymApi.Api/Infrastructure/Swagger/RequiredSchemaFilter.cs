using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace GymApi.Api.Infrastructure.Swagger;

// ReSharper disable once ClassNeverInstantiated.Global
public class RequiredSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema || concreteSchema.Properties == null)
        {
            return;
        }

        // Handle properties from the type itself
        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // ReSharper disable once InvertIf
            if (property.GetCustomAttribute<RequiredAttribute>() != null)
            {
                var schemaProperty = concreteSchema.Properties.FirstOrDefault(p => p.Key.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
                // ReSharper disable once InvertIf
                if (schemaProperty.Key != null)
                {
                    if (schemaProperty.Value is OpenApiSchema { Type: not null } propertySchema)
                    {
                        propertySchema.Type &= ~JsonSchemaType.Null;
                    }
                    
                    concreteSchema.Required ??= new HashSet<string>();
                    concreteSchema.Required.Add(schemaProperty.Key);
                }
            }
        }

        // Handle properties from record primary constructor parameters (if applicable)
        // This is a more robust way to handle [Required] on record properties
        foreach (var parameter in context.Type.GetConstructors()
                     .SelectMany(c => c.GetParameters())
                     .Where(p => p.GetCustomAttribute<RequiredAttribute>() != null))
        {
            var schemaProperty = concreteSchema.Properties.FirstOrDefault(p => p.Key.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));
            if (schemaProperty.Key == null)
            {
                continue;
            }

            if (schemaProperty.Value is OpenApiSchema { Type: not null } propertySchema)
            {
                propertySchema.Type &= ~JsonSchemaType.Null;
            }
            
            concreteSchema.Required ??= new HashSet<string>();
            concreteSchema.Required.Add(schemaProperty.Key);
        }
    }
}
