$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$testDir = Join-Path ([IO.Path]::GetTempPath()) ('SpineBoundsCheck_' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDir | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$testExe = Join-Path $testDir 'BoundsTests.exe'
& $compiler /nologo /target:exe /main:BoundsTests "/out:$testExe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "$repoRoot\src\SpineCadenceStudio.cs" "$PSScriptRoot\BoundsTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'Bounds test compilation failed.' }
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'Bounds tests failed.' }
