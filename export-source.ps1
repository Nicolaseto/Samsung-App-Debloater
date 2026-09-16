param([string]$Destination = 'Samsung-App-Debloater')
$ErrorActionPreference = 'Stop'
$sourceRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$destinationPath = [IO.Path]::GetFullPath((Join-Path $sourceRoot $Destination))
if (-not $destinationPath.StartsWith($sourceRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Destino deve ficar dentro da pasta do projeto.' }
if (Test-Path -LiteralPath $destinationPath) { throw 'O destino já existe. Use outro nome com -Destination; nada foi apagado.' }
$publicFiles = @('.gitignore','.gitattributes','LICENSE','README.md','CONTRIBUTING.md','THIRD_PARTY_NOTICES.md','build.ps1','test.ps1','export-source.ps1','Catalog.cs','Core.cs','Debloater.cs','Cleanup.cs','DemoInventory.cs','Tests.cs','docs/PUBLICAR.md','.github/workflows/build.yml')
foreach ($relativePath in $publicFiles) {
    $inputPath = Join-Path $sourceRoot $relativePath
    if (-not (Test-Path -LiteralPath $inputPath -PathType Leaf)) { throw "Arquivo obrigatório ausente: $relativePath" }
}
foreach ($relativePath in $publicFiles) {
    $targetPath = Join-Path $destinationPath $relativePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $targetPath) | Out-Null
    Copy-Item -LiteralPath (Join-Path $sourceRoot $relativePath) -Destination $targetPath
}
Write-Output "Código-fonte exportado: $destinationPath"
$publicFiles
