using System.Threading;
using System.Threading.Tasks;

namespace VmPortal.Application.Console
{
    public interface IConsoleSessionService
    {
        public Task<string> CreateSessionAsync(
            string node,
            int vmId,
            string ownerExternalId,
            CancellationToken ct = default);

        public ConsoleSession? GetSession(string sessionId);

        public void InvalidateSession(string sessionId);
    }
}
