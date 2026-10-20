using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html lang='uk'>
<head>
    <meta charset='utf-8'/>
    <title>NovaGames API Gateway</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #0f172a; color: #f8fafc; padding: 40px; margin: 0; }
        .card { background: #1e293b; border-radius: 12px; padding: 24px; max-width: 700px; margin: 0 auto; box-shadow: 0 10px 25px rgba(0,0,0,0.5); border: 1px solid #334155; }
        h1 { color: #38bdf8; margin-top: 0; }
        p { color: #94a3b8; font-size: 16px; }
        ul { list-style: none; padding: 0; }
        li { margin-bottom: 12px; }
        a { display: inline-block; padding: 10px 16px; background: #2563eb; color: #fff; text-decoration: none; border-radius: 8px; font-weight: 600; transition: background 0.2s; }
        a:hover { background: #1d4ed8; }
        .badge { display: inline-block; padding: 4px 8px; border-radius: 4px; font-size: 12px; margin-left: 8px; font-weight: bold; background: #334155; color: #38bdf8; }
        .code-block { background: #090d16; padding: 12px; border-radius: 6px; font-family: monospace; color: #a5f3fc; font-size: 13px; margin-top: 15px; }
    </style>
</head>
<body>
    <div class='card'>
        <h1>🎮 NovaGames API Gateway (Port 5000)</h1>
        <p>Шлюз Ocelot успішно запущено. Оберіть документацію API для тестування:</p>
        <ul>
            <li>
                <a href='http://localhost:5001/scalar/v1' target='_blank'>AuthService API (Scalar UI)</a>
                <span class='badge'>Port 5001</span>
            </li>
            <li>
                <a href='http://localhost:5002/scalar/v1' target='_blank'>CatalogService API (Scalar UI)</a>
                <span class='badge'>Port 5002</span>
            </li>
            <li>
                <a href='http://localhost:5042/swagger' target='_blank'>Monolith API (Swagger UI)</a>
                <span class='badge'>Port 5042</span>
            </li>
            <li>
                <a href='/health' style='background: #10b981;'>Gateway Health Check</a>
                <span class='badge'>/health</span>
            </li>
        </ul>
        <div class='code-block'>
            Клієнтські запити через шлюз:<br/>
            POST http://localhost:5000/api/v1/auth/register<br/>
            POST http://localhost:5000/api/v1/auth/login<br/>
            GET  http://localhost:5000/api/v1/catalog/items<br/>
            GET  http://localhost:5000/api/v1/games
        </div>
    </div>
</body>
</html>");
        return;
    }

    if (context.Request.Path == "/health")
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("Gateway is healthy!");
        return;
    }

    var stopwatch = Stopwatch.StartNew();

    if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
    {
        correlationId = Guid.NewGuid().ToString();
        context.Request.Headers.TryAdd("X-Correlation-Id", correlationId);
    }

    context.Response.Headers.TryAdd("X-Correlation-Id", correlationId);

    await next.Invoke();

    stopwatch.Stop();

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {context.Request.Method} {context.Request.Path} -> Status: {context.Response.StatusCode} | Time: {stopwatch.ElapsedMilliseconds}ms | CorrID: {correlationId}");
});

await app.UseOcelot();

app.Run();
