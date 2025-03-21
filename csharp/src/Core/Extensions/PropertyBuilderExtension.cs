using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Train.Solver.Core.Extensions;
public static class PropertyBuilderExtension
{
    public static PropertyBuilder<T> HasEnumComment<T>(this PropertyBuilder<T> propertyBuilder) where T : Enum
    {
        var enumValues = Enum.GetValues(typeof(T)).Cast<T>().ToList();
        return propertyBuilder.HasComment(string.Join(',', enumValues.Select(x => $"{x}={(int)(object)x}")));
    }
}