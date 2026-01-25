using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VmPortal.Application.Proxmox;
using VmPortal.Domain.Vms;

namespace VmPortal.Infrastructure.Proxmox
{
    internal class ProxmoxClient : IProxmoxClient
    {
        private readonly HttpClient _http;
        private readonly ProxmoxOptions _opt;

        public ProxmoxClient(HttpClient http, IOptions<ProxmoxOptions> options)
        {
            _http = http;
            _opt = options.Value;

            // Ensure auth header is present with current secrets
            if (!_http.DefaultRequestHeaders.Contains("Authorization"))
            {
                _http.DefaultRequestHeaders.Add("Authorization", $"PVEAPIToken {_opt.TokenId}={_opt.TokenSecret}");
            }
        }

        public async Task<VmStatus> GetVmStatusAsync(string node, int vmId, CancellationToken ct = default)
        {
            var url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/status/current";
            using var resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();

            var doc = await resp.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            var status = doc?.RootElement.GetProperty("data").GetProperty("status").GetString();

            return status switch
            {
                "running" => VmStatus.Running,
                "stopped" => VmStatus.Stopped,
                "paused" => VmStatus.Paused,
                _ => VmStatus.Unknown
            };
        }

        public async Task StartVmAsync(string node, int vmId, CancellationToken ct = default)
        {
            var url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/status/start";
            using var resp = await _http.PostAsync(url, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task StopVmAsync(string node, int vmId, CancellationToken ct = default)
        {
            var url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/status/stop";
            using var resp = await _http.PostAsync(url, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task ShutdownVmAsync(string node, int vmId, CancellationToken ct = default)
        {
            var url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/status/shutdown";
            using var resp = await _http.PostAsync(url, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }
    }
}
