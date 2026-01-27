using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VmPortal.Domain.Security;

namespace VmPortal.Application.Security
{
    public interface ISecurityEventLogger
    {
        Task LogSecurityEventAsync(SecurityEvent securityEvent);
        Task LogLoginAttemptAsync(string userId, string ipAddress, bool success, string details = "");
        Task LogSessionViolationAsync(string userId, string sessionId, string reason);
        Task LogUnauthorizedAccessAsync(string userId, string resource, string ipAddress);
    }
}
