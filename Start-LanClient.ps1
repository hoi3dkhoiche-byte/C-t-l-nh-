param(
    [string]$ServerUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"
$normalized = $ServerUrl.Trim()
if ($normalized -and $normalized -notmatch '^https?://') {
    $normalized = "http://$normalized"
}

$parsedAddress = $null
if (-not [Uri]::TryCreate($normalized, [UriKind]::Absolute, [ref]$parsedAddress) -or
    $parsedAddress.Scheme -notin @("http", "https")) {
    throw "ServerUrl phai la HTTP/HTTPS, vi du http://192.168.1.85:5000."
}

$env:COTULENH_SERVER_URL = $normalized
$publishedClient = Join-Path $PSScriptRoot "ClientAPP.exe"
if (Test-Path -LiteralPath $publishedClient) {
    $clientProcess = Start-Process -FilePath $publishedClient -WorkingDirectory $PSScriptRoot -Wait -PassThru
    $clientExitCode = $clientProcess.ExitCode
}
else {
    $clientProject = Join-Path $PSScriptRoot "CoTuLenh.Client\ClientAPP\ClientAPP.csproj"
    & dotnet run --project $clientProject
    $clientExitCode = $LASTEXITCODE
}
if ($clientExitCode -ne 0) { throw "Client stopped with exit code $clientExitCode." }
