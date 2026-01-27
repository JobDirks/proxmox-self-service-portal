using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VmPortal.Application.Security;
using VmPortal.Domain.Security;
using VmPortal.Infrastructure.Data;

namespace VmPortal.Infrastructure.Security
{
    internal class SecurityEventLogger : ISecurityEventLogger
    {
        private readonly VmPortalDbContext _dbContext;
        private readonly ILogger<SecurityEventLogger> _logger;

        public SecurityEventLogger(VmPortalDbContext dbContext, ILogger<SecurityEventLogger> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
        {
            try
            {
                _dbContext.SecurityEvents.Add(securityEvent);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Security event logged: {EventType} for user {UserId}",
                    securityEvent.EventType, securityEvent.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event: {EventType}", securityEvent.EventType);
            }
        }

        public async Task LogLoginAttemptAsync(string userId, string ipAddress, bool success, string details = "")
        {
            var securityEvent = new SecurityEvent
            {
                Id = Guid.NewGuid(),
                EventType = success ? "LoginSuccess" : "LoginFailed",
                UserId = userId,
                IpAddress = ipAddress,
                Details = details,
                Severity = success ? "Information" : "Warning",
                OccurredAt = DateTimeOffset.UtcNow
            };

            await LogSecurityEventAsync(securityEvent);
        }

        public async Task LogSessionViolationAsync(string userId, string sessionId, string reason)
        {
            var securityEvent = new SecurityEvent
            {
                Id = Guid.NewGuid(),
                EventType = "SessionViolation",
                UserId = userId,
                Details = $"SessionId: {sessionId}, Reason: {reason}",
                Severity = "Error",
                OccurredAt = DateTimeOffset.UtcNow
            };

            await LogSecurityEventAsync(securityEvent);
        }

        public async Task LogUnauthorizedAccessAsync(string userId, string resource, string ipAddress)
        {
            var securityEvent = new SecurityEvent
            {
                Id = Guid.NewGuid(),
                EventType = "UnauthorizedAccess",
                UserId = userId,
                IpAddress = ipAddress,
                Details = $"Resource: {resource}",
                Severity = "Warning",
                OccurredAt = DateTimeOffset.UtcNow
            };

            await LogSecurityEventAsync(securityEvent);
        }
    }
}
