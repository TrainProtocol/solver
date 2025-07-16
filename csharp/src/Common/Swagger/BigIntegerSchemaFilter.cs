using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Numerics;

namespace Train.Solver.Common.Swagger;

public class BigIntegerSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(BigInteger))
        {
            schema.Type = "string";
            schema.Format = null;
            schema.Example = new OpenApiString("123456789");
        }
    }
}