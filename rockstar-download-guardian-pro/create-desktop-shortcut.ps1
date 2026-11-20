# Executar apenas quando o usuário pedir a criação do atalho na Área de Trabalho.
$ErrorActionPreference = 'Stop'
$exe = Join-Path $PSScriptRoot 'publish\Rockstar Download Guardian Pro.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Compile primeiro usando build.ps1.' }
$desktop = [Environment]::GetFolderPath('DesktopDirectory')
$linkPath = Join-Path $desktop 'Rockstar Download Guardian Pro.lnk'
$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($linkPath)
$link.TargetPath = $exe
$link.WorkingDirectory = Split-Path -Parent $exe
$link.Description = 'Monitoramento local do Rockstar Launcher'
$link.Save()
Write-Output 'Atalho criado na Área de Trabalho.'
