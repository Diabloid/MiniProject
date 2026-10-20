using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.Use(async (context, next) =>
{
    // 1. Перехоплюємо /health власноруч до Ocelot
    if (context.Request.Path == "/health")
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("Gateway is healthy!");
        return; // Зупиняємо конвеєр тут, Ocelot цей запит не побачить
    }

    // 2. Логування та Correlation-Id для всіх інших запитів
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