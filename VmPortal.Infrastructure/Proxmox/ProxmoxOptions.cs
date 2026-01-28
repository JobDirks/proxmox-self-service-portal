namespace VmPortal.Infrastructure.Proxmox
{
    public class ProxmoxOptions
    {
        public string BaseUrl { get; set; } = string.Empty;     // e.g. https://172.25.75.132:8006
        public string TokenId { get; set; } = string.Empty;     // e.g. root@pam!TokenID
        public string TokenSecret { get; set; } = string.Empty; // secret
        public bool DevIgnoreCertErrors { get; set; } = true;   // DEV ONLY; disable in production

        public string DefaultDisk { get; set; } = "scsi0";
    }
}
