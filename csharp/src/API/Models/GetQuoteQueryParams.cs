using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Train.Solver.API.Models;

public class GetQuoteQueryParams : GetRouteLimitsQueryParams
{
    [FromQuery(Name = "amount")]
    public decimal? Amount { get; set; }
}

public class GetQuoteQueryParamsValidator : AbstractValidator<GetQuoteQueryParams>
{
    public GetQuoteQueryParamsValidator()
    {
        RuleFor(x => x.SourceNetwork).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.SourceAsset).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.DestinationNetwork).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.DestinationAsset).NotNull().NotEmpty().MaximumLength(255);
        RuleFor(x => x.Amount).NotNull().GreaterThan(0);
    }
}
