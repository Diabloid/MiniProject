# ==============================================================================
# NovaGames Microservices Stopper
# Зупиняє всі запущені процеси dotnet для бекенду
# ==============================================================================

Write-Host "Зупинка мікросервісів та API Gateway..." -ForegroundColor Yellow

$ports = @(5000, 5001, 5002, 5003, 5042)
foreach ($port in $ports) {
    $conn = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($conn) {
        $pids = $conn.OwningProcess | Select-Object -Unique
        foreach ($procId in $pids) {
            try {
                Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
                Write-Host "Зупинено процес на порті $port (PID: $procId)" -ForegroundColor Green
            } catch {}
        }
    }
}

Write-Host "Усі сервіси зупинено." -ForegroundColor Cyan
