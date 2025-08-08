using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Train.Solver.Common.Serialization;

public class BigIntegerConverter : JsonConverter<BigInteger>
{
    public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => BigInteger.Parse(reader.GetString()!),
            JsonTokenType.Number => reader.TryGetInt64(out var longValue)
                ? new BigInteger(longValue)
                : BigInteger.Parse(reader.GetDouble().ToString("R")), // For large numbers in double form
            _ => throw new JsonException($"Unexpected token parsing BigInteger. Token: {reader.TokenType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}