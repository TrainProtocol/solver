using System.Text.Json;
using System.Text.Json.Serialization;

namespace Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models.Converters;

public class StringEnumToLowerConverter : JsonConverter<FunctionName>
{
    public override FunctionName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Enum.TryParse<FunctionName>(reader.GetString(), true, out var val) ? val : default;
    }

    public override void Write(Utf8JsonWriter writer, FunctionName value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(char.ToLowerInvariant(value.ToString()[0]) + value.ToString()[1..]);
    }

}
