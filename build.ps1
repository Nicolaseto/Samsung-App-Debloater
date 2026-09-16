param([string]$OutputFile = 'Samsung-App-Debloater.exe')
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe "/out:$OutputFile" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll Debloater.cs Core.cs Catalog.cs Cleanup.cs DemoInventory.cs Tests.cs
if ($LASTEXITCODE -ne 0) { throw 'Falha na compilação' }
