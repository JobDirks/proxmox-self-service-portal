namespace VmPortal.Application.Proxmox
{
    public sealed class ProxmoxVncProxyInfo
    {
        public int Port { get; set; }
        public string Ticket { get; set; } = string.Empty;
    }
}
