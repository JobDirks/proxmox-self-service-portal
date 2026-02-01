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

        private readonly ProxmoxConsoleOptions _consoleOpt;

        public ProxmoxClient(HttpClient http, IOptions<ProxmoxOptions> options, IOptions<ProxmoxConsoleOptions> consoleOptions)
        {
            _http = http;
            _opt = options.Value;
            _consoleOpt = consoleOptions.Value;

            if (!_http.DefaultRequestHeaders.Contains("Authorization"))
            {
                _http.DefaultRequestHeaders.Add("Authorization", $"PVEAPIToken {_opt.TokenId}={_opt.TokenSecret}");
            }
        }

        public async Task<string> GetLoginTicketAsync(CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_consoleOpt.Username) ||
                string.IsNullOrWhiteSpace(_consoleOpt.Password))
            {
                throw new InvalidOperationException("Proxmox console credentials are not configured.");
            }

            string baseUrl = _opt.BaseUrl.TrimEnd('/');
            Uri baseUri = new Uri(baseUrl + "/api2/json/");

            HttpClientHandler handler = new HttpClientHandler();
            if (_opt.DevIgnoreCertErrors)
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            using HttpClient localHttp = new HttpClient(handler)
            {
                BaseAddress = baseUri
            };

            Dictionary<string, string> form = new Dictionary<string, string>
            {
                ["username"] = _consoleOpt.Username,
                ["password"] = _consoleOpt.Password
            };

            HttpContent content = new FormUrlEncodedContent(form);

            using HttpResponseMessage resp = await localHttp.PostAsync("access/ticket", content, ct);
            string responseBody = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Proxmox access/ticket failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {responseBody}");
            }

            JsonDocument doc = JsonDocument.Parse(responseBody);
            JsonElement data = doc.RootElement.GetProperty("data");

            string? ticket = data.GetProperty("ticket").GetString();
            if (string.IsNullOrEmpty(ticket))
            {
                throw new InvalidOperationException("Proxmox access/ticket response is missing 'ticket'.");
            }

            return ticket;
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

        public async Task RebootVmAsync(string node, int vmId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(node))
            {
                throw new ArgumentException("Node is required.", nameof(node));
            }

            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/status/reboot";
            using HttpResponseMessage resp = await _http.PostAsync(url, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task ResetVmAsync(string node, int vmId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(node))
            {
                throw new ArgumentException("Node is required.", nameof(node));
            }

            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/status/reset";
            using HttpResponseMessage resp = await _http.PostAsync(url, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task PauseVmAsync(string node, int vmId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(node))
            {
                throw new ArgumentException("Node is required.", nameof(node));
            }

            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/status/suspend";
            using HttpResponseMessage resp = await _http.PostAsync(url, content: null, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task ResumeVmAsync(string node, int vmId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(node))
            {
                throw new ArgumentException("Node is required.", nameof(node));
            }

            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/status/resume";
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

        public async Task ConfigureVmResourcesAsync(
            string node,
            int vmId,
            int cpuCores,
            int memoryMiB,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(node))
            {
                throw new ArgumentException("Node is required.", nameof(node));
            }

            if (vmId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(vmId), "VMID must be positive.");
            }

            // Build form only with values we actually want to change
            Dictionary<string, string> form = new Dictionary<string, string>();

            if (cpuCores > 0)
            {
                form["cores"] = cpuCores.ToString();
            }

            if (memoryMiB > 0)
            {
                form["memory"] = memoryMiB.ToString();
            }

            // If nothing to change, do nothing
            if (form.Count == 0)
            {
                return;
            }

            HttpContent content = new FormUrlEncodedContent(form);
            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/config";

            using HttpResponseMessage resp = await _http.PostAsync(url, content, ct);
            string responseBody = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Proxmox config failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {responseBody}");
            }
        }


        private async Task<string?> GetPrimaryDiskNameAsync(string node, int vmId, CancellationToken ct)
        {
            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/config";
            using HttpResponseMessage resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();

            JsonDocument? doc = await resp.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            if (doc == null)
            {
                return null;
            }

            JsonElement data = doc.RootElement.GetProperty("data");

            // Common disk keys in order of preference
            string[] candidates = new[] { "scsi0", "virtio0", "sata0", "ide0", "efidisk0" };

            foreach (string key in candidates)
            {
                if (data.TryGetProperty(key, out JsonElement _))
                {
                    return key;
                }
            }

            return null;
        }


        public async Task ResizeVmDiskAsync(
            string node,
            int vmId,
            int newDiskGiB,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(node))
            {
                throw new ArgumentException("Node is required.", nameof(node));
            }

            if (vmId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(vmId), "VMID must be positive.");
            }

            if (newDiskGiB <= 0)
            {
                return;
            }

            // Determine which disk to resize
            string? diskName;

            if (!string.IsNullOrWhiteSpace(_opt.DefaultDisk))
            {
                diskName = _opt.DefaultDisk;
            }
            else
            {
                diskName = await GetPrimaryDiskNameAsync(node, vmId, ct);
            }

            if (string.IsNullOrWhiteSpace(diskName))
            {
                throw new InvalidOperationException(
                    $"Could not determine primary disk for VM {vmId} on node {node}.");
            }

            // Absolute new size, e.g. "60G"
            string sizeArgument = newDiskGiB.ToString() + "G";

            Dictionary<string, string> form = new Dictionary<string, string>
            {
                ["disk"] = diskName,
                ["size"] = sizeArgument
            };

            HttpContent content = new FormUrlEncodedContent(form);
            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/resize";

            // IMPORTANT: use PUT, not POST
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = content
            };

            using HttpResponseMessage resp = await _http.SendAsync(request, ct);
            string responseBody = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Proxmox disk resize failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {responseBody}");
            }
        }

        public async Task<ProxmoxVncProxyInfo> CreateVncProxyAsync(
            string node,
            int vmId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(node))
            {
                throw new ArgumentException("Node is required.", nameof(node));
            }

            if (vmId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(vmId), "VMID must be positive.");
            }

            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/vncproxy";
            // Proxmox expects a POST; an empty form body is fine
            HttpContent content = new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>());

            using HttpResponseMessage resp = await _http.PostAsync(url, content, ct);
            string responseBody = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Proxmox VNC proxy failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {responseBody}");
            }

            JsonDocument doc = JsonDocument.Parse(responseBody);
            JsonElement data = doc.RootElement.GetProperty("data");

            string? portString = data.GetProperty("port").GetString();
            string? ticket = data.GetProperty("ticket").GetString();

            if (string.IsNullOrEmpty(portString) || string.IsNullOrEmpty(ticket))
            {
                throw new InvalidOperationException("Proxmox VNC proxy response is missing port or ticket.");
            }

            if (!int.TryParse(portString, out int port))
            {
                throw new InvalidOperationException($"Invalid VNC port returned from Proxmox: '{portString}'.");
            }

            ProxmoxVncProxyInfo info = new ProxmoxVncProxyInfo
            {
                Port = port,
                Ticket = ticket
            };

            return info;
        }

        public async Task<IReadOnlyList<ProxmoxVmInfo>> ListVmsAsync(CancellationToken ct = default)
        {
            // Use /cluster/resources?type=vm to get all VMs (qemu + lxc); we filter to qemu
            const string url = "cluster/resources?type=vm";

            using HttpResponseMessage resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();

            JsonDocument? doc = await resp.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            if (doc == null)
            {
                throw new InvalidOperationException("Failed to parse Proxmox cluster/resources response.");
            }

            JsonElement root = doc.RootElement;
            if (!root.TryGetProperty("data", out JsonElement dataArray) || dataArray.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Proxmox cluster/resources response is missing 'data' array.");
            }

            List<ProxmoxVmInfo> result = new List<ProxmoxVmInfo>();

            foreach (JsonElement item in dataArray.EnumerateArray())
            {
                // Only qemu VMs for now
                string? type = item.TryGetProperty("type", out JsonElement typeElement)
                    ? typeElement.GetString()
                    : null;

                if (!string.Equals(type, "qemu", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? node = item.TryGetProperty("node", out JsonElement nodeElement)
                    ? nodeElement.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(node))
                {
                    continue;
                }

                int vmId;
                if (!item.TryGetProperty("vmid", out JsonElement vmidElement) || !vmidElement.TryGetInt32(out vmId))
                {
                    continue;
                }

                string name = item.TryGetProperty("name", out JsonElement nameElement)
                    ? (nameElement.GetString() ?? string.Empty)
                    : string.Empty;

                string statusString = item.TryGetProperty("status", out JsonElement statusElement)
                    ? (statusElement.GetString() ?? string.Empty)
                    : string.Empty;

                string tagsString = item.TryGetProperty("tags", out JsonElement tagsElement)
                    ? (tagsElement.GetString() ?? string.Empty)
                    : string.Empty;

                List<string> tags = new List<string>();
                if (!string.IsNullOrWhiteSpace(tagsString))
                {
                    string[] split = tagsString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (string tag in split)
                    {
                        string trimmed = tag.Trim();
                        if (trimmed.Length > 0)
                        {
                            tags.Add(trimmed);
                        }
                    }
                }

                // Resource parsing
                int cpuCores = 0;
                if (item.TryGetProperty("maxcpu", out JsonElement maxCpuElement) &&
                    maxCpuElement.TryGetInt32(out int cpuVal))
                {
                    cpuCores = cpuVal;
                }

                int memoryMiB = 0;
                if (item.TryGetProperty("maxmem", out JsonElement maxMemElement))
                {
                    long memBytes;
                    if (maxMemElement.ValueKind == JsonValueKind.Number &&
                        maxMemElement.TryGetInt64(out memBytes))
                    {
                        memoryMiB = (int)(memBytes / (1024L * 1024L));
                    }
                }

                int diskGiB = 0;
                if (item.TryGetProperty("maxdisk", out JsonElement maxDiskElement))
                {
                    long diskBytes;
                    if (maxDiskElement.ValueKind == JsonValueKind.Number &&
                        maxDiskElement.TryGetInt64(out diskBytes))
                    {
                        diskGiB = (int)(diskBytes / (1024L * 1024L * 1024L));
                    }
                }

                VmStatus status = statusString switch
                {
                    "running" => VmStatus.Running,
                    "stopped" => VmStatus.Stopped,
                    "paused" => VmStatus.Paused,
                    _ => VmStatus.Unknown
                };

                ProxmoxVmInfo info = new ProxmoxVmInfo
                {
                    Node = node,
                    VmId = vmId,
                    Name = name,
                    Status = status,
                    Tags = tags,
                    CpuCores = cpuCores,
                    MemoryMiB = memoryMiB,
                    DiskGiB = diskGiB
                };

                result.Add(info);
            }

            return result;
        }

        public async Task SetVmTagsAsync(string node, int vmId, IEnumerable<string> tags, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(node))
            {
                throw new ArgumentException("Node is required.", nameof(node));
            }

            if (vmId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(vmId), "VMID must be positive.");
            }

            // First, get existing tags from VM config
            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/config";
            using HttpResponseMessage getResp = await _http.GetAsync(url, ct);
            getResp.EnsureSuccessStatusCode();

            JsonDocument? doc = await getResp.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            HashSet<string> mergedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (doc != null)
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("data", out JsonElement dataElement))
                {
                    if (dataElement.TryGetProperty("tags", out JsonElement tagsElement))
                    {
                        string? existingTagString = tagsElement.GetString();
                        if (!string.IsNullOrWhiteSpace(existingTagString))
                        {
                            string[] existingTags = existingTagString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                            foreach (string tag in existingTags)
                            {
                                string trimmed = tag.Trim();
                                if (trimmed.Length > 0)
                                {
                                    mergedTags.Add(trimmed);
                                }
                            }
                        }
                    }
                }
            }

            // Add new tags
            foreach (string t in tags)
            {
                if (!string.IsNullOrWhiteSpace(t))
                {
                    string trimmed = t.Trim();
                    if (trimmed.Length > 0)
                    {
                        mergedTags.Add(trimmed);
                    }
                }
            }

            string tagsValue = string.Join(";", mergedTags);

            Dictionary<string, string> form = new Dictionary<string, string>
            {
                ["tags"] = tagsValue
            };

            HttpContent content = new FormUrlEncodedContent(form);
            string postUrl = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/config";

            using HttpResponseMessage resp = await _http.PostAsync(postUrl, content, ct);
            string responseBody = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Proxmox SetVmTags failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {responseBody}");
            }
        }

        public async Task DeleteVmAsync(string node, int vmId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(node))
            {
                throw new ArgumentException("Node is required.", nameof(node));
            }

            if (vmId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(vmId), "VMID must be positive.");
            }

            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}";
            using HttpResponseMessage resp = await _http.DeleteAsync(url, ct);
            resp.EnsureSuccessStatusCode();
        }
    }
}
