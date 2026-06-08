# Exports swagger/v1/swagger.json from a running TodoPlatform.Api instance.
# Usage: .\scripts\export-openapi.ps1 [-Port 5099] [-OutFile artifacts\swagger-v1.json]

param(
    [int]$Port = 5099,
    [string]$OutFile = "artifacts\swagger-v1.json"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\TodoPlatform.Api\TodoPlatform.Api.csproj"
$outPath = if ([System.IO.Path]::IsPathRooted($OutFile)) { $OutFile } else { Join-Path $root $OutFile }
$outDir = Split-Path -Parent $outPath

if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Database__UseInMemory = "true"
$env:Database__AutoMigrate = "false"

$process = Start-Process -FilePath "dotnet" `
    -ArgumentList "run", "--project", $project, "--no-build", "--urls", "http://127.0.0.1:$Port" `
    -PassThru -WindowStyle Hidden

try {
    $deadline = (Get-Date).AddSeconds(30)
    do {
        Start-Sleep -Milliseconds 500
        try {
            $response = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/swagger/v1/swagger.json" -UseBasicParsing
            if ($response.StatusCode -eq 200) { break }
        }
        catch {
            if ((Get-Date) -gt $deadline) {
                throw "Timed out waiting for swagger at http://127.0.0.1:$Port/swagger/v1/swagger.json"
            }
        }
    } while ($true)

    Invoke-WebRequest -Uri "http://127.0.0.1:$Port/swagger/v1/swagger.json" -OutFile $outPath
    Write-Host "Exported OpenAPI to $outPath"
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}
