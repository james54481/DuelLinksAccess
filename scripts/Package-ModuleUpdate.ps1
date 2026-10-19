param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$project=[xml](Get-Content -LiteralPath (Join-Path $repo 'DuelLinksAccess.csproj') -Raw)
$version=$project.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if($version -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'Expected a four-part community version' }
$main=Get-Content -LiteralPath (Join-Path $repo 'src/Main.cs') -Raw
if($main -notmatch ('"DuelLinksAccess",\s*"'+[regex]::Escape($version)+'"')) { throw 'MelonInfo version mismatch' }
$dll=Join-Path $repo 'bin/Release/net6.0/DuelLinksAccess.dll'
if([Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString() -ne $version) { throw 'Built DLL version mismatch; rebuild before packaging' }
if(Test-Path -LiteralPath $OutputDirectory) { throw 'Choose a new package staging directory' }
$mods=Join-Path $OutputDirectory 'Mods'
New-Item -ItemType Directory -Path $mods -Force | Out-Null
Copy-Item -LiteralPath $dll -Destination $mods
foreach($name in @('LICENSE.txt','CHANGELOG.zh-TW.md')) { Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $OutputDirectory }
$instructions=@"
DuelLinksAccess 繁體中文維護版 $version

本套件只供已安裝 DuelLinksAccess 的使用者更新，不含首次安裝所需元件。
先完全關閉 Duel Links 並備份現有 DLL，再將 Mods/DuelLinksAccess.dll 放入遊戲的 Mods 資料夾。
也可使用獨立更新程式自動下載、驗證、備份與替換 DLL。
首次安裝與更新程式說明：https://github.com/james54481/DuelLinksAccess/tree/zh-tw
上游作者與授權見 LICENSE.txt，修改說明見 CHANGELOG.zh-TW.md。
"@
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'README.txt'),$instructions)
$checks=Get-ChildItem -LiteralPath $OutputDirectory -File -Recurse | ForEach-Object {
    (Get-FileHash -LiteralPath $_.FullName).Hash+'  '+[IO.Path]::GetRelativePath($OutputDirectory,$_.FullName).Replace('\','/')
}
[IO.File]::WriteAllLines((Join-Path $OutputDirectory 'SHA256SUMS.txt'),$checks)
$zip=Join-Path (Split-Path -Parent $OutputDirectory) "DuelLinksAccess-v$version-zhTW.zip"
Compress-Archive -Path (Join-Path $OutputDirectory '*') -DestinationPath $zip -Force
Write-Output $zip
