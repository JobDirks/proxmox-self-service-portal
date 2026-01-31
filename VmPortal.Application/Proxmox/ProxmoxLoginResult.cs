namespace VmPortal.Application.Proxmox
{
    public sealed class ProxmoxLoginResult
    {
        public string Ticket { get; set; } = string.Empty;
        public string CsrfToken { get; set; } = string.Empty;
    }
}
