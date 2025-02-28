using System.Linq.Expressions;
using System.Reflection;
using FluentValidation.Internal;
using Train.Solver.Core.Extensions;

namespace Train.Solver.API.Validators;

public class SnakeCasePropertyResolver
{
    public static string ResolvePropertyName(Type type, MemberInfo memberInfo, LambdaExpression expression)
    {
        return DefaultPropertyNameResolver(memberInfo, expression).ToSnakeCase();
    }

    private static string? DefaultPropertyNameResolver(MemberInfo memberInfo, LambdaExpression expression)
    {
        if (expression != null)
        {
            var chain = PropertyChain.FromExpression(expression);
            if (chain.Count > 0) return chain.ToString();
        }

        if (memberInfo != null)
        {
            return memberInfo.Name;
        }

        return null;
    }
}
