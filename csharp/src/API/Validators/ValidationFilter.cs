using FluentResults;
using FluentValidation;
using Train.Solver.API.Extensions;
using Train.Solver.Core.Errors;

namespace Train.Solver.API.Validators;

public class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator = validator ?? throw new ArgumentNullException(nameof(validator));

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validatable = context.Arguments.SingleOrDefault(x => x?.GetType() == typeof(T)) as T;

        if (validatable is not null)
        {
            var validationResult = await _validator.ValidateAsync(validatable);

            if (!validationResult.IsValid)
            {
                return Result.Fail(new ValidationError(validationResult.Errors.First().ErrorMessage))
                    .ToHttpResult();
            }
        }

        return await next(context);
    }
}
