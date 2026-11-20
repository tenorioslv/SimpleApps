$ErrorActionPreference = 'Stop'
$appRoot = $PSScriptRoot
$sdk = Join-Path $appRoot '.tools\dotnet\dotnet.exe'
# O resolvedor de SDK do MSBuild pode falhar em caminhos longos. Um mapeamento
# temporário aponta para a mesma pasta, sem copiar ou mover arquivos.
if ($appRoot.Length -gt 100 -and (Test-Path -LiteralPath $sdk)) {
    $drive = @('Z:', 'Y:', 'X:', 'W:') | Where-Object { -not (Test-Path ($_ + '\')) } | Select-Object -First 1
    if (-not $drive) { throw 'Nenhuma unidade temporária disponível para encurtar o caminho do SDK.' }
    subst.exe $drive (Split-Path -Parent $appRoot)
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível criar o mapeamento temporário.' }
    try {
        $shortRoot = Join-Path ($drive + '\') (Split-Path -Leaf $appRoot)
        Push-Location $shortRoot
        try { & (Join-Path $shortRoot 'build.ps1') } finally { Pop-Location }
    } finally { subst.exe $drive /D }
    return
}
if (-not (Test-Path -LiteralPath $sdk)) { $sdk = (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_HOME = Join-Path $appRoot '.tools\cli-home'
$env:NUGET_PACKAGES = Join-Path $appRoot '.tools\nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_NOLOGO = '1'
& $sdk run --project (Join-Path $appRoot 'tests\Guardian.Tests.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Testes falharam.' }
& $sdk publish (Join-Path $appRoot 'RockstarGuardian.csproj') -c Release -r win-x64 --self-contained true -o (Join-Path $appRoot 'publish') -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'Publicação falhou.' }
