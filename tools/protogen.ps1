
$protoPath = "../protos"
$protoFile = "blockchain.proto"
$tempDir = "./__temp_proto_out"
$outputPath = "../csharp/src/Core/Activities/IBlockchainActivities.gen.cs"

mkdir $tempDir -ErrorAction Ignore

./protoc.exe `
  --proto_path=$protoPath `
  --csharp_out=$tempDir `
  "$protoPath/$protoFile"

Move-Item "$tempDir/*.cs" $outputPath -Force
Remove-Item $tempDir -Recurse -Force

Write-Host "C# code generated: $outputPath"