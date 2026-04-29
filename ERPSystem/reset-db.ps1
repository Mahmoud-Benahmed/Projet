param (
    [string]$InfraProject = $null,
    [string]$StartupProject = $null,
    [string]$DbContext = $null,
    [string]$MigrationName = $null
)

# =========================
# LOAD CONFIG
# =========================
$currentDir = Get-Location
$appSettingsPath = Join-Path $currentDir "appsettings.json"

if (-not (Test-Path $appSettingsPath)) {
    throw "appsettings.json not found in $currentDir"
}

$appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
$efReset = $appSettings.EfReset

# =========================
# APPLY DEFAULTS
# =========================
if (-not $InfraProject) { $InfraProject = $efReset.Project }
if (-not $StartupProject) { $StartupProject = $efReset.Project }
if (-not $DbContext) { $DbContext = $efReset.DbContext }
if (-not $MigrationName) { $MigrationName = $efReset.MigrationName }

# =========================
# RESOLVE PATHS
# =========================
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$infraPath = Resolve-Path (Join-Path $scriptRoot $InfraProject)
$startupPath = Resolve-Path (Join-Path $scriptRoot $StartupProject)

Write-Host "Infra Project: $infraPath"
Write-Host "Startup Project: $startupPath"

# =========================
# DROP DB
# =========================
Write-Host "Dropping database..." -ForegroundColor Yellow
dotnet ef database drop --force `
  --project $infraPath `
  --startup-project $startupPath `
  --context $DbContext

# =========================
# DELETE MIGRATIONS
# =========================
$migrationsPath = Join-Path (Split-Path $infraPath) "Migrations"

Write-Host "Deleting Migrations folder..." -ForegroundColor Yellow
if (Test-Path $migrationsPath) {
    Remove-Item -Recurse -Force $migrationsPath
    Write-Host "Migrations folder deleted." -ForegroundColor DarkYellow
} else {
    Write-Host "No Migrations folder found." -ForegroundColor DarkYellow
}

# =========================
# ADD MIGRATION
# =========================
Write-Host "Adding migration: $MigrationName..." -ForegroundColor Yellow
dotnet ef migrations add $MigrationName `
  --project $infraPath `
  --startup-project $startupPath `
  --context $DbContext

# =========================
# UPDATE DB
# =========================
Write-Host "Updating database..." -ForegroundColor Yellow
dotnet ef database update `
  --project $infraPath `
  --startup-project $startupPath `
  --context $DbContext

Write-Host "Done." -ForegroundColor Green