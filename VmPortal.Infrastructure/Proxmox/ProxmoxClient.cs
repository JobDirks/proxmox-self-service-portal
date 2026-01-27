using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
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

            if (!_http.DefaultRequestHeaders.Contains("Authorization"))
            {
                _http.DefaultRequestHeaders.Add("Authorization", $"PVEAPIToken {_opt.TokenId}={_opt.TokenSecret}");
            }
        }

        public async Task<VmStatus> GetVmStatusAsync(string node, int vmId, CancellationToken ct = default)
        {
            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/status/current";
            using HttpResponseMessage resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();

            JsonDocument? doc = await resp.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            string? status = doc?.RootElement.GetProperty("data").GetProperty("status").GetString();

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
            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/status/start";
            using HttpResponseMessage resp = await _http.PostAsync(url, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task StopVmAsync(string node, int vmId, CancellationToken ct = default)
        {
            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/status/stop";
            using HttpResponseMessage resp = await _http.PostAsync(url, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task ShutdownVmAsync(string node, int vmId, CancellationToken ct = default)
        {
            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/status/shutdown";
            using HttpResponseMessage resp = await _http.PostAsync(url, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task<int> GetNextVmIdAsync(CancellationToken ct = default)
        {
            string url = "cluster/nextid";
            using HttpResponseMessage resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();

            JsonDocument? doc = await resp.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            if (doc == null)
            {
                throw new InvalidOperationException("Failed to parse Proxmox nextid response.");
            }

            string? idString = doc.RootElement.GetProperty("data").GetString();
            if (!int.TryParse(idString, out int vmId))
            {
                throw new InvalidOperationException($"Invalid VMID returned from Proxmox nextid: '{idString}'.");
            }

            return vmId;
        }

        public async Task<ProxmoxCloneResult> CloneVmAsync(
            string node,
            int templateVmId,
            string newName,
            int cpuCores,
            int memoryMiB,
            int diskGiB,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(node))
            {
                throw new ArgumentException("Node is required.", nameof(node));
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("New VM name is required.", nameof(newName));
            }

            if (templateVmId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(templateVmId), "Template VMID must be positive.");
            }

            int newVmId = await GetNextVmIdAsync(ct);

            Dictionary<string, string> form = new Dictionary<string, string>
            {
                ["name"] = newName,
                ["target"] = node,
                ["full"] = "1",                 // full clone
                ["newid"] = newVmId.ToString()  // required by your cluster
            };

            // cpuCores, memoryMiB, diskGiB are not sent here; Proxmox rejects them on /clone in your environment.

            HttpContent content = new FormUrlEncodedContent(form);
            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{templateVmId}/clone";

            using HttpResponseMessage resp = await _http.PostAsync(url, content, ct);
            string responseBody = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Proxmox clone failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {responseBody}");
            }

            JsonDocument doc = JsonDocument.Parse(responseBody);
            string taskId = string.Empty;
            if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement))
            {
                string? upid = dataElement.GetString();
                taskId = upid ?? string.Empty;
            }

            ProxmoxCloneResult result = new ProxmoxCloneResult
            {
                VmId = newVmId,
                TaskId = taskId
            };

            return result;
        }
    }
}
