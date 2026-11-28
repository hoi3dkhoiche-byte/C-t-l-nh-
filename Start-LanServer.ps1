param(
    [int]$Port = 5000
)

$ErrorActionPreference = "Stop"
$serverDirectory = Join-Path $PSScriptRoot "cotulenh_server"
$pythonExecutable = Join-Path $serverDirectory ".venv\Scripts\python.exe"
if (-not (Test-Path -LiteralPath $pythonExecutable)) {
    throw "Chua co Python venv. Xem huong dan cai server trong README.md."
}

$env:HOST = "0.0.0.0"
$env:PORT = $Port.ToString()
Push-Location $serverDirectory
try {
    & $pythonExecutable -m flask --app main db upgrade
    if ($LASTEXITCODE -ne 0) { throw "Database migration failed." }
    Write-Host "LAN server: port $Port. Hai client phai cung tro ve IP cua may nay."
    & $pythonExecutable main.py
    if ($LASTEXITCODE -ne 0) { throw "Server stopped with exit code $LASTEXITCODE." }
}
finally {
    Pop-Location
}
