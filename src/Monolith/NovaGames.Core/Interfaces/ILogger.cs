using NovaGames.Core.Models;
using System.Runtime.CompilerServices;

namespace NovaGames.Core.Interfaces
{
    public interface ILogger
    {
        // Асинхронний метод
        Task LogAsync(string message, LogType type,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string filePath = "");
    }
}