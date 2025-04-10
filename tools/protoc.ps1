# Paths
$protoRoot = "..\protos"
$protoFile = Join-Path $protoRoot "blockchain.proto"
$outputDir = ".\"
$pluginPath = ".\protoc-gen-js.exe"

# Run protoc
.\protoc.exe `
  --plugin=protoc-gen-js=$pluginPath `
  --js_out=import_style=commonjs,binary:$outputDir `
  -I $protoRoot `
  $protoFile

Read-Host -Prompt "Done. Press Enter to exit"