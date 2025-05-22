using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace Train.Solver.API.Models;

public class GetQuoteQueryParams : GetRouteLimitsQueryParams
{
    [SwaggerParameter(Required = true)]
    public decimal? Amount { get; set; }
}

public class GetQuoteQueryParamsValidator : AbstractValidator<GetQuoteQueryParams>
{
    public GetQuoteQueryParamsValidator()
    {
        RuleFor(x => x.SourceNetwork).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.SourceToken).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.DestinationNetwork).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.DestinationToken).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.Amount).NotNull().GreaterThan(0);
    }
}
