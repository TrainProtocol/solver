using System.Linq.Expressions;
using System.Reflection;

namespace Train.Solver.Common.Helpers;

public static class ExpressionHelper
{
    public static (MethodInfo Method, IReadOnlyCollection<object?> Args) ExtractCall<TDelegate>(
        Expression<TDelegate> expr, bool errorSaysPropertyAccepted = false)
    {
        // Body must be a method call
        if (expr.Body is not MethodCallExpression call)
        {
            if (errorSaysPropertyAccepted)
            {
                throw new ArgumentException("Expression must be a single method call or property access");
            }
            throw new ArgumentException("Expression must be a single method call");
        }
        // The LHS of the method, if non-static, must be the parameter
        if (call.Object == null && expr.Parameters.Count > 0)
        {
            throw new ArgumentException("Static call expression must not have a lambda parameter");
        }
        if (call.Object != null)
        {
            if (expr.Parameters.Count != 1 ||
                expr.Parameters.Single() is not ParameterExpression paramExpr ||
                paramExpr.IsByRef ||
                call.Object != paramExpr)
            {
                throw new ArgumentException(
                    "Instance call expression must have a single lambda parameter used for the call");
            }
        }

        var args = call.Arguments.Select(e =>
        {
            if (e is ConstantExpression constExpr)
            {
                return constExpr.Value;
            }
            var expr = Expression.Lambda<Func<object?>>(Expression.Convert(e, typeof(object)));

#if NET471_OR_GREATER
            return expr.Compile(true)();
#else
            return expr.Compile()();
#endif
        });
        return (call.Method, args.ToArray());
    }
}
