using System.Text.Json;
using System.Text.Json.Serialization;
using Train.Solver.Core.Blockchains.Solana.Models;

namespace Train.Solver.Core.Blockchains.Solana.Helpers;

public class ParsedInstructionDataConverter : JsonConverter<ParsedInstructionData>
{
    public override ParsedInstructionData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);

        var root = jsonDoc.RootElement;
        if (root.ValueKind == JsonValueKind.Object)
        {
            return JsonSerializer.Deserialize<ParsedInstructionData>(root.GetRawText())!;
        }
        return null;
    }

    public override void Write(Utf8JsonWriter writer, ParsedInstructionData value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class InstructionDataConverter : JsonConverter<Instruction>
{
    public override Instruction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        if (root.TryGetProperty("program", out JsonElement programElement) &&
            SolanaConstants.memoType == programElement.GetString() && 
            root.TryGetProperty("parsed", out JsonElement memoElement))
        {
            return new()
            {
                Parsed = new ParsedInstructionData()
                {
                    Memo = memoElement.GetString()
                }
            };
        }
        else
        {
            return JsonSerializer.Deserialize<Instruction>(root.GetRawText())!;
        }
    }

    public override void Write(Utf8JsonWriter writer, Instruction value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}


public class ParsedBlockInstructionDataConverter : JsonConverter<ParsedInstructionData>
{
    public override ParsedInstructionData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("type", out JsonElement typeElement) &&
            SolanaConstants.transferTypes.Contains(typeElement.GetString()))
        {
            return JsonSerializer.Deserialize<ParsedInstructionData>(root.GetRawText())!;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, ParsedInstructionData value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}