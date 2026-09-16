param([switch]$NoUI, [string]$Executable = 'Samsung-App-Debloater.exe')
$ErrorActionPreference = 'Stop'
$executablePath = Join-Path $PSScriptRoot $Executable
if (-not (Test-Path -LiteralPath $executablePath)) { throw 'Compile primeiro com build.ps1.' }
$testArgs = @('--self-test')
if ($NoUI) { $testArgs += '--no-ui' }
$testProcess = Start-Process -FilePath $executablePath -ArgumentList $testArgs -WindowStyle Hidden -PassThru -Wait
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'test-results.txt')
if ($testProcess.ExitCode -ne 0) { throw "Testes falharam: $($testProcess.ExitCode)" }
