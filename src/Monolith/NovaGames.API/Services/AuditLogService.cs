using NovaGames.Core.Entities;
using NovaGames.API.Data;

namespace NovaGames.API.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(int? userId, string actionType, string details)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                ActionType = actionType,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}