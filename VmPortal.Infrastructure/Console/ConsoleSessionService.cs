using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;
using VmPortal.Application.Console;
using VmPortal.Application.Proxmox;

namespace VmPortal.Infrastructure.Console
{
    internal sealed class ConsoleSessionService : IConsoleSessionService
    {
        private readonly IProxmoxClient _proxmoxClient;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _sessionLifetime;

        public ConsoleSessionService(IProxmoxClient proxmoxClient, IMemoryCache cache)
        {
            _proxmoxClient = proxmoxClient;
            _cache = cache;
            _sessionLifetime = TimeSpan.FromMinutes(5);
        }

        public async Task<string> CreateSessionAsync(
            string node,
            int vmId,
            string ownerExternalId,
            CancellationToken ct = default)
        {
            ProxmoxVncProxyInfo proxyInfo = await _proxmoxClient.CreateVncProxyAsync(node, vmId, ct);

            string sessionId = Guid.NewGuid().ToString("N");
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(_sessionLifetime);

            ConsoleSession session = new ConsoleSession
            {
                SessionId = sessionId,
                Node = node,
                VmId = vmId,
                Port = proxyInfo.Port,
                Ticket = proxyInfo.Ticket,
                ExpiresAt = expiresAt,
                OwnerExternalId = ownerExternalId
            };

            MemoryCacheEntryOptions options = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt
            };

            _cache.Set(sessionId, session, options);

            return sessionId;
        }

        public ConsoleSession? GetSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return null;
            }

            if (_cache.TryGetValue(sessionId, out ConsoleSession? session))
            {
                if (session != null && session.ExpiresAt > DateTimeOffset.UtcNow)
                {
                    return session;
                }

                _cache.Remove(sessionId);
            }

            return null;
        }

        public void InvalidateSession(string sessionId)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                _cache.Remove(sessionId);
            }
        }
    }
}
