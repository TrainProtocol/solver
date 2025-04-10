using ProtoBuf;
using ProtoBuf.Meta;
using System.Reflection;
using Train.Solver.Blockchain.Abstractions.Models;

var types = Assembly.GetAssembly(typeof(BalanceRequest))!
    .GetTypes()
    .Where(t => t.GetCustomAttribute<ProtoContractAttribute>() != null)
    .ToList();

var generatorOptions = new SchemaGenerationOptions();
generatorOptions.Types.AddRange(types);

var schema = Serializer.GetProto(generatorOptions);

var protoPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "../../../../../../protos/blockchain.proto"));

Directory.CreateDirectory(Path.GetDirectoryName(protoPath)!);
File.WriteAllText(protoPath, schema);

public interface klir<T> {
    void Estimate(T a);

}

public class klirimpl : klir<string>
{
    public void Estimate(string a)
    {
        throw new NotImplementedException();
    }
}