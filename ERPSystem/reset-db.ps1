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
# RESOLVE PATHS (SMART)
# =========================
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

function Resolve-CsprojPath {
    param (
        [string]$inputPath
    )

    # 1️⃣ Try direct path (relative to script root)
    $fullPath = Join-Path $scriptRoot $inputPath
    if (Test-Path $fullPath) {
        if ($fullPath.EndsWith(".csproj")) {
            return (Resolve-Path $fullPath).Path
        }

        # If it's a folder → find .csproj inside
        $csproj = Get-ChildItem -Path $fullPath -Filter *.csproj -Recurse | Select-Object -First 1
        if ($csproj) {
            return $csproj.FullName
        }
    }

    # 2️⃣ Search entire solution (fallback)
    Write-Host "Searching for $inputPath in solution..." -ForegroundColor DarkYellow

    $found = Get-ChildItem -Path $scriptRoot -Recurse -Filter *.csproj |
        Where-Object { $_.Name -eq $inputPath } |
        Select-Object -First 1

    if ($found) {
        return $found.FullName
    }

    throw "Could not resolve project: $inputPath"
}

$infraPath = Resolve-CsprojPath $InfraProject
$startupPath = Resolve-CsprojPath $StartupProject

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
$projectDir = Split-Path $infraPath -Parent
$migrationsPath = Join-Path $projectDir "Migrations"

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