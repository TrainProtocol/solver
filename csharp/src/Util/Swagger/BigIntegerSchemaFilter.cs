using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Util.Swagger;

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