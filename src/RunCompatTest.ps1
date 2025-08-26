param()
$ErrorActionPreference = 'Stop'

Write-Host 'Building AkavacheV10Writer and AkavacheV11Reader...'
dotnet build .\src\AkavacheV10Writer\AkavacheV10Writer.csproj -c Release
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet build .\src\AkavacheV11Reader\AkavacheV11Reader.csproj -c Release
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host 'Running AkavacheV10Writer...'
dotnet run --no-build -c Release -p .\src\AkavacheV10Writer\AkavacheV10Writer.csproj
$writerExit = $LASTEXITCODE

Write-Host 'Running AkavacheV11Reader...'
dotnet run --no-build -c Release -p .\src\AkavacheV11Reader\AkavacheV11Reader.csproj
$readerExit = $LASTEXITCODE

if ($readerExit -eq 0) {
  Write-Host "\nAll compatibility checks passed."
  exit 0
} else {
  Write-Host "\nCompatibility check failures encountered."
  exit 1
}
