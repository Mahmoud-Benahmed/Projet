$projectRoot = Get-Location
$infraProject = "ERP.StockService.Infrastructure"
$startupProject = "ERP.StockService"
$dbContext = "StockDbContext"
$connectionString = "Server=localhost,1438;Database=StockServiceDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True"

Write-Host "Dropping database..." -ForegroundColor Yellow
dotnet ef database drop --force

Write-Host "Removing migrations folder..." -ForegroundColor Yellow
dotnet ef migrations remove

Write-Host "Adding InitialCreate migration..." -ForegroundColor Yellow
dotnet ef migrations add InitialCreate

Write-Host "Updating database..." -ForegroundColor Yellow
dotnet ef database update

Write-Host "Done." -ForegroundColor Green