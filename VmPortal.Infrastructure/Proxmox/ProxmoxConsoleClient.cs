using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VmPortal.Application.Proxmox;

namespace VmPortal.Infrastructure.Proxmox
{
    internal sealed class ProxmoxConsoleClient : IProxmoxConsoleClient
    {
        private readonly ProxmoxOptions _opt;
        private readonly ProxmoxConsoleOptions _consoleOpt;

        public ProxmoxConsoleClient(
            IOptions<ProxmoxOptions> opt,
            IOptions<ProxmoxConsoleOptions> consoleOpt)
        {
            _opt = opt.Value;
            _consoleOpt = consoleOpt.Value;
        }

        public async Task<ProxmoxLoginResult> LoginAsync(CancellationToken ct = default)
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

            using HttpClient http = new HttpClient(handler)
            {
                BaseAddress = baseUri
            };

            Dictionary<string, string> form = new Dictionary<string, string>
            {
                ["username"] = _consoleOpt.Username,
                ["password"] = _consoleOpt.Password
            };

            HttpContent content = new FormUrlEncodedContent(form);

            using HttpResponseMessage resp = await http.PostAsync("access/ticket", content, ct);
            string responseBody = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Proxmox access/ticket failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {responseBody}");
            }

            JsonDocument doc = JsonDocument.Parse(responseBody);
            JsonElement data = doc.RootElement.GetProperty("data");

            string? ticket = data.GetProperty("ticket").GetString();
            string? csrfToken = data.GetProperty("CSRFPreventionToken").GetString();

            if (string.IsNullOrEmpty(ticket) || string.IsNullOrEmpty(csrfToken))
            {
                throw new InvalidOperationException("Proxmox access/ticket response is missing 'ticket' or 'CSRFPreventionToken'.");
            }

            ProxmoxLoginResult result = new ProxmoxLoginResult
            {
                Ticket = ticket,
                CsrfToken = csrfToken
            };

            return result;
        }

        public async Task<ProxmoxVncProxyInfo> CreateVncProxyAsync(
            string node,
            int vmId,
            ProxmoxLoginResult login,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(node))
            {
                throw new ArgumentException("Node is required.", nameof(node));
            }

            string baseUrl = _opt.BaseUrl.TrimEnd('/');
            Uri baseUri = new Uri(baseUrl + "/api2/json/");

            HttpClientHandler handler = new HttpClientHandler();
            if (_opt.DevIgnoreCertErrors)
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            using HttpClient http = new HttpClient(handler)
            {
                BaseAddress = baseUri
            };

            // Attach login ticket + CSRF to this session
            http.DefaultRequestHeaders.Add("Cookie", "PVEAuthCookie=" + login.Ticket);
            http.DefaultRequestHeaders.Add("CSRFPreventionToken", login.CsrfToken);

            string url = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmId}/vncproxy";

            // IMPORTANT: request websocket proxy, just like the PHP example
            Dictionary<string, string> form = new Dictionary<string, string>
            {
                ["websocket"] = "1"
            };

            HttpContent content = new FormUrlEncodedContent(form);

            using HttpResponseMessage resp = await http.PostAsync(url, content, ct);
            string responseBody = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Proxmox vncproxy failed: {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {responseBody}");
            }

            JsonDocument doc = JsonDocument.Parse(responseBody);
            JsonElement data = doc.RootElement.GetProperty("data");

            string? portString = data.GetProperty("port").GetString();
            string? vncTicket = data.GetProperty("ticket").GetString();

            if (string.IsNullOrEmpty(portString) || string.IsNullOrEmpty(vncTicket))
            {
                throw new InvalidOperationException("Proxmox vncproxy response missing port or ticket.");
            }

            int port;
            if (!int.TryParse(portString, out port))
            {
                throw new InvalidOperationException($"Invalid VNC port returned from Proxmox: '{portString}'.");
            }

            ProxmoxVncProxyInfo info = new ProxmoxVncProxyInfo
            {
                Port = port,
                Ticket = vncTicket
            };

            return info;
        }
    }
}
