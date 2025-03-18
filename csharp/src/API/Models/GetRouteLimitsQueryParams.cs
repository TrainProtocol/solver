using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace Train.Solver.API.Models;

public class GetRouteLimitsQueryParams
{
    [SwaggerParameter(Required = true)]
    public string? SourceNetwork { get; set; }

    [SwaggerParameter(Required = true)]
    public string? SourceToken { get; set; }

    [SwaggerParameter(Required = true)]
    public string? DestinationNetwork { get; set; }

    [SwaggerParameter(Required = true)]
    public string? DestinationToken { get; set; }
}

public class GetRouteLimitsQueryParamsValidator : AbstractValidator<GetRouteLimitsQueryParams>
{
    public GetRouteLimitsQueryParamsValidator()
    {
        RuleFor(x => x.SourceNetwork).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.SourceToken).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.DestinationNetwork).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.DestinationToken).NotNull().NotEmpty().MaximumLength(255);
    }
}
