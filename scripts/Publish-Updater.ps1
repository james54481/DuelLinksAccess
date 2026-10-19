param(
    [string]$Dotnet = 'dotnet',
    [string]$OutputDirectory,
    [string]$PackageCache = $env:NUGET_PACKAGES
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'dist/updater-0.1.0-win-x64' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$project = Join-Path $repo 'updater/DuelLinksAccess.Updater/DuelLinksAccess.Updater.csproj'
& $Dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:UseSharedCompilation=false `
    -p:NuGetAudit=false `
    -nodeReuse:false -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw 'Updater publish failed.' }
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE.txt') -Destination $OutputDirectory
Copy-Item -LiteralPath (Join-Path $repo 'docs/UPDATER.zh-TW.md') -Destination (Join-Path $OutputDirectory '使用說明.md')
if (-not $PackageCache) { $PackageCache = Join-Path $env:USERPROFILE '.nuget/packages' }
$licenses = Join-Path $OutputDirectory 'third-party-licenses'
New-Item -ItemType Directory -Path $licenses -Force | Out-Null
$runtimeConfig = Get-Content -LiteralPath (Join-Path $repo 'updater/DuelLinksAccess.Updater/bin/Release/net8.0-windows/win-x64/DuelLinksAccess.Updater.runtimeconfig.json') -Raw | ConvertFrom-Json
foreach ($package in @('microsoft.netcore.app.runtime.win-x64','microsoft.windowsdesktop.app.runtime.win-x64')) {
    $frameworkName = if ($package -like 'microsoft.netcore.*') { 'Microsoft.NETCore.App' } else { 'Microsoft.WindowsDesktop.App' }
    $version = ($runtimeConfig.runtimeOptions.includedFrameworks | Where-Object name -EQ $frameworkName).version
    if (-not $version) { throw "Missing bundled runtime version for $frameworkName" }
    $files = Get-ChildItem -LiteralPath (Join-Path $PackageCache "$package/$version") -File | Where-Object Name -Match '^(LICENSE(\.TXT)?|THIRD-PARTY-NOTICES\.TXT)$'
    if (-not $files) { throw "Missing runtime notices for $package" }
    foreach ($file in $files) { Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $licenses ($package + '-' + $file.Name)) }
}
$exe = Join-Path $OutputDirectory 'DuelLinksAccess.Updater.exe'
$hash = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'SHA256.txt'), "$hash  DuelLinksAccess.Updater.exe`r`n")
$zip = $OutputDirectory + '.zip'
Compress-Archive -Path (Join-Path $OutputDirectory '*') -DestinationPath $zip -Force
Write-Output "Updater: $exe"
Write-Output "Package: $zip"
