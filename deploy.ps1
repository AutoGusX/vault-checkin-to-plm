# Builds the extension and copies it into the Vault 2027 extensions folder.
# Vault client and Inventor load extensions at startup, so restart them afterwards.
param(
    [string]$ExtensionsRoot = "C:\ProgramData\Autodesk\Vault 2027\Extensions"
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $repo "src\CheckinToPlm\CheckinToPlm.csproj"
$output = Join-Path $repo "src\CheckinToPlm\bin\Release\net10.0-windows"
$target = Join-Path $ExtensionsRoot "CheckinToPlm"

dotnet build $project -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

New-Item -ItemType Directory -Force $target | Out-Null
foreach ($name in "CheckinToPlm.dll", "CheckinToPlm.pdb", "CheckinToPlm.vcet.config") {
    Copy-Item (Join-Path $output $name) $target -Force
}

Write-Host "Deployed to $target"
Get-ChildItem $target | ForEach-Object { Write-Host "  $($_.Name)  $($_.LastWriteTime)" }
