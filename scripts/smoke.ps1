#!/usr/bin/env pwsh
# Smoke for Windows hosts (same checks as smoke.sh)
$ErrorActionPreference = "Stop"

$ApiUrl = if ($env:API_URL) { $env:API_URL } else { "http://localhost:8080" }
$KeycloakUrl = if ($env:KEYCLOAK_URL) { $env:KEYCLOAK_URL } else { "http://localhost:8180" }
$Realm = if ($env:KEYCLOAK_REALM) { $env:KEYCLOAK_REALM } else { "todo-platform" }
$ClientId = if ($env:KEYCLOAK_CLIENT_ID) { $env:KEYCLOAK_CLIENT_ID } else { "todo-spa" }
$Username = if ($env:KEYCLOAK_USERNAME) { $env:KEYCLOAK_USERNAME } else { "test@example.com" }
$Password = if ($env:KEYCLOAK_PASSWORD) { $env:KEYCLOAK_PASSWORD } else { "password123" }

Write-Host "==> Waiting for API ready at $ApiUrl/health/ready"
$ready = $false
for ($i = 1; $i -le 90; $i++) {
    try {
        $r = Invoke-WebRequest -Uri "$ApiUrl/health/ready" -UseBasicParsing -TimeoutSec 5
        if ($r.StatusCode -eq 200) { $ready = $true; break }
    } catch { }
    Start-Sleep -Seconds 2
}
if (-not $ready) { throw "API health check timed out." }
Write-Host "API healthy."

$tokenUrl = "$KeycloakUrl/realms/$Realm/protocol/openid-connect/token"
Write-Host "==> Waiting for Keycloak token"
$token = $null
for ($i = 1; $i -le 90; $i++) {
    try {
        $body = @{
            client_id  = $ClientId
            grant_type = "password"
            username   = $Username
            password   = $Password
        }
        $resp = Invoke-RestMethod -Method Post -Uri $tokenUrl -Body $body -ContentType "application/x-www-form-urlencoded"
        $token = $resp.access_token
        if ($token) { break }
    } catch { }
    Start-Sleep -Seconds 2
}
if (-not $token) { throw "Keycloak token endpoint timed out." }
Write-Host "Keycloak issued token."

Write-Host "==> GET $ApiUrl/api/todos"
$headers = @{ Authorization = "Bearer $token" }
$todos = Invoke-WebRequest -Uri "$ApiUrl/api/todos" -Headers $headers -UseBasicParsing
if ($todos.StatusCode -ne 200) { throw "GET /api/todos failed with HTTP $($todos.StatusCode)" }
Write-Host "Smoke OK (GET /api/todos → 200)."
