$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$source = Join-Path $repoRoot 'src\SpineCadenceStudio.cs'
$text = Get-Content -LiteralPath $source -Raw -Encoding UTF8
$version = [regex]::Match($text, 'public const string Version="([^"]+)"').Groups[1].Value
if (!$version) { throw 'Version not found.' }
$targetDir = Join-Path $repoRoot "dist\SpineCadenceStudio_$version"
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $compiler)) { throw '.NET Framework C# compiler not found.' }
& $compiler /nologo /target:winexe /platform:x64 "/win32manifest:$repoRoot\src\app.manifest" "/out:$targetDir\SpineCadenceStudio.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll $source
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Copy-Item -LiteralPath "$repoRoot\src\SpineCadenceStudio.exe.config" -Destination $targetDir
Copy-Item -LiteralPath "$repoRoot\README.md" -Destination $targetDir
Get-FileHash -LiteralPath "$targetDir\SpineCadenceStudio.exe" -Algorithm SHA256
