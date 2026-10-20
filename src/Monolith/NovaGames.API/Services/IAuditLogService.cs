namespace NovaGames.API.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(int? userId, string actionType, string details);
    }
}
