# ==============================================================================
# NovaGames Microservices Runner
# Запускає всі бекенд-сервіси та API Gateway в окремих вікнах PowerShell
# ==============================================================================

Write-Host "Перевірка запуску MySQL..." -ForegroundColor Cyan
$mysqlService = Get-Service -Name "MySQL80" -ErrorAction SilentlyContinue
if ($mysqlService -and $mysqlService.Status -ne "Running") {
    Write-Host "Запуск служби MySQL80..." -ForegroundColor Yellow
    Start-Service -Name "MySQL80"
}
Write-Host "MySQL працює!" -ForegroundColor Green

$rootDir = $PSScriptRoot

$services = @(
    @{ Name = "AuthService (5001)"; Path = "$rootDir\src\AuthService" },
    @{ Name = "CatalogService (5002)"; Path = "$rootDir\src\CatalogService" },
    @{ Name = "OrderingService (5003)"; Path = "$rootDir\src\OrderingService" },
    @{ Name = "Monolith API (5042)"; Path = "$rootDir\src\Monolith\NovaGames.API" },
    @{ Name = "API Gateway (5000)"; Path = "$rootDir\src\ApiGateway" }
)

foreach ($svc in $services) {
    Write-Host "Запуск $($svc.Name)..." -ForegroundColor Yellow
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$($svc.Path)'; Write-Host '--- $($svc.Name) ---' -ForegroundColor Green; dotnet run"
}

Write-Host "`nУсі сервіси запущено!" -ForegroundColor Green
Write-Host "API Gateway доступний на: http://localhost:5000" -ForegroundColor Cyan
Write-Host "Статус та документація: http://localhost:5000/scalar/v1" -ForegroundColor Cyan
