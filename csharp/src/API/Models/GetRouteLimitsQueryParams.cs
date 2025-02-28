using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Train.Solver.API.Models;

public class GetRouteLimitsQueryParams
{
    [FromQuery(Name = "source_network")]
    [SwaggerParameter(Required = true)]
    public string? SourceNetwork { get; set; }

    [FromQuery(Name = "source_token")]
    [SwaggerParameter(Required = true)]
    public string? SourceAsset { get; set; }

    [FromQuery(Name = "destination_network")]
    [SwaggerParameter(Required = true)]
    public string? DestinationNetwork { get; set; }

    [FromQuery(Name = "destination_token")]
    [SwaggerParameter(Required = true)]
    public string? DestinationAsset { get; set; }
}

public class GetRouteLimitsQueryParamsValidator : AbstractValidator<GetRouteLimitsQueryParams>
{
    public GetRouteLimitsQueryParamsValidator()
    {
        RuleFor(x => x.SourceNetwork).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.SourceAsset).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.DestinationNetwork).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.DestinationAsset).NotNull().NotEmpty().MaximumLength(255);
    }
}
